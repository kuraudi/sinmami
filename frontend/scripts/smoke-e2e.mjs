const apiBase = process.env.API_BASE_URL ?? "http://127.0.0.1:5099/api";
const frontendBase = process.env.FRONTEND_BASE_URL ?? "http://127.0.0.1:3007";

function ensure(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function wait(ms) {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

async function requestJson(path, { method = "GET", plan = "Free", body } = {}) {
  const response = await fetch(`${apiBase}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json; charset=utf-8",
      "X-Plan": plan,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await response.text();
  const payload = text ? JSON.parse(text) : null;

  if (!response.ok) {
    throw new Error(`${method} ${path} -> ${response.status} ${text}`);
  }

  return payload;
}

async function requestText(url) {
  const response = await fetch(url);
  const text = await response.text();

  if (!response.ok) {
    throw new Error(`GET ${url} -> ${response.status}`);
  }

  return text;
}

async function requestBinary(url) {
  const response = await fetch(url);
  const buffer = new Uint8Array(await response.arrayBuffer());

  if (!response.ok) {
    throw new Error(`GET ${url} -> ${response.status}`);
  }

  return buffer;
}

async function withRetry(action, attempts = 3, delayMs = 5000) {
  let lastError;

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await action();
    } catch (error) {
      lastError = error;
      if (attempt < attempts) {
        await wait(delayMs);
      }
    }
  }

  throw lastError;
}

async function fillDraft(draftId, plan, answers) {
  for (const [stepKey, value] of answers) {
    await requestJson(`/drafts/${draftId}/answers`, {
      method: "POST",
      plan,
      body: { stepKey, value },
    });
  }
}

async function runPremiumFlow() {
  const plan = "Premium";
  const draft = await requestJson("/drafts", {
    method: "POST",
    plan,
    body: { documentType: 1 },
  });

  const answers = [
    ["landlord_type", "individual"],
    ["landlord_rf_citizenship_confirmed", true],
    ["landlord_name", "Иванов Илья Сергеевич"],
    ["landlord_passport_number", "45 01 123456"],
    ["landlord_passport_issued_by", "ОВД Тверского района г. Москвы"],
    ["landlord_passport_unit_code", "770-001"],
    ["landlord_passport_issue_date", "15.07.2018"],
    ["landlord_registration_address", "г. Москва, ул. Бутырская, д. 7, кв. 18"],
    ["landlord_phone", "+7 916 000-00-01"],
    ["tenant_type", "individual"],
    ["tenant_rf_citizenship_confirmed", true],
    ["tenant_name", "Петрова Анна Дмитриевна"],
    ["tenant_passport_number", "45 02 654321"],
    ["tenant_passport_issued_by", "ОВД Пресненского района г. Москвы"],
    ["tenant_passport_unit_code", "770-002"],
    ["tenant_passport_issue_date", "22.03.2020"],
    ["tenant_registration_address", "г. Москва, ул. Пресненская, д. 12, кв. 44"],
    ["tenant_phone", "+7 916 000-00-02"],
    ["agreement_city", "Москва"],
    ["agreement_date", "10.04.2026"],
    ["property_type", "apartment"],
    ["property_address", "г. Москва, ул. Лесная, д. 15, кв. 27"],
    ["property_description", "Двухкомнатная квартира с мебелью и бытовой техникой, готова к проживанию."],
    ["property_area_sqm", 58],
    ["lease_start_date", "10.04.2026"],
    ["lease_end_date", "09.04.2027"],
    ["rent_amount", 85000],
    ["payment_due_day", 5],
    ["payment_method", "cash"],
    ["utilities_payment_terms", "Арендатор отдельно оплачивает коммунальные услуги по квитанциям и показаниям счетчиков."],
  ];

  await fillDraft(draft.draftId, plan, answers);

  const draftPage = await requestText(`${frontendBase}/drafts/${draft.draftId}`);
  ensure(draftPage.includes("Пошаговое заполнение договора аренды"), "Draft page did not render expected title.");

  const ask = await withRetry(() =>
    requestJson(`/drafts/${draft.draftId}/ask`, {
      method: "POST",
      plan,
      body: {
        stepKey: "payment_method",
        question: "Зачем вообще указывать способ оплаты в договоре и как это лучше сформулировать?",
      },
    }),
  );

  ensure(ask.answer.length > 40, "AI help returned an unexpectedly short answer.");
  ensure(!ask.answer.includes("**"), "AI help should not contain markdown markers.");
  ensure(!ask.answer.toLowerCase().includes("think"), "AI help should not expose reasoning text.");

  const validation = await requestJson(`/drafts/${draft.draftId}/validate`, {
    method: "POST",
    plan,
    body: {},
  });
  ensure(validation.isValid === true, "Premium draft failed validation.");

  const generation = await withRetry(() =>
    requestJson(`/drafts/${draft.draftId}/generate`, {
      method: "POST",
      plan,
      body: {
        includeGuide: true,
        requestedAppendices: [],
      },
    }),
  );

  const document = await requestJson(`/documents/${generation.documentId}`, { plan });
  const guide = await requestJson(`/documents/${generation.documentId}/guide`, { plan });

  ensure(!document.title.includes("?"), "Premium document title contains question marks instead of text.");
  ensure(document.title.includes("Лесная"), "Premium document title did not preserve the address.");
  ensure([1, 2].includes(guide.guideType), "Premium guide should return either personalized content or a standard fallback.");

  const handoverFlow = await requestJson(`/documents/${generation.documentId}/appendices/flow/1`, { plan });
  const petFlow = await requestJson(`/documents/${generation.documentId}/appendices/flow/3`, { plan });

  ensure(handoverFlow.steps.length > 0, "Handover appendix flow should return steps.");
  ensure(petFlow.steps.length > 0, "Pet appendix flow should return steps.");

  const appendixHelp = await withRetry(() =>
    requestJson(`/documents/${generation.documentId}/appendices/ask`, {
      method: "POST",
      plan,
      body: {
        appendixType: 1,
        stepKey: "handover_meter_readings",
        question: "Объясни простыми словами, что именно сюда писать и зачем нужны показания счетчиков.",
        answers: {
          handover_transfer_date: "10.04.2026",
        },
      },
    }),
  );

  ensure(appendixHelp.answer.length > 40, "Appendix AI help returned an unexpectedly short answer.");
  ensure(!appendixHelp.answer.includes("**"), "Appendix AI help should not contain markdown markers.");

  const appendixActPreview = await requestJson(`/documents/${generation.documentId}/appendices/preview`, {
    method: "POST",
    plan,
    body: {
      appendixType: 1,
      answers: {
        handover_transfer_date: "10.04.2026",
        handover_property_condition: "Квартира чистая, техника исправна, объект готов к передаче.",
        handover_visible_defects: "Незначительная потертость на ручке межкомнатной двери.",
        handover_keys_transferred: "2 ключа от квартиры, 1 ключ от почтового ящика, 1 брелок от подъезда.",
        handover_meter_readings: "Электричество 15420 кВт·ч, холодная вода 124 м3, горячая вода 98 м3."
      }
    },
  });

  ensure(appendixActPreview.isReady === true, "Handover appendix preview should be ready.");

  const appendixAct = await withRetry(() =>
    requestJson(`/documents/${generation.documentId}/appendices`, {
      method: "POST",
      plan,
      body: {
        appendixType: 1,
        answers: {
          handover_transfer_date: "10.04.2026",
          handover_property_condition: "Квартира чистая, техника исправна, объект готов к передаче.",
          handover_visible_defects: "Незначительная потертость на ручке межкомнатной двери.",
          handover_keys_transferred: "2 ключа от квартиры, 1 ключ от почтового ящика, 1 брелок от подъезда.",
          handover_meter_readings: "Электричество 15420 кВт·ч, холодная вода 124 м3, горячая вода 98 м3."
        }
      },
    }),
  );

  const appendixPetPreview = await requestJson(`/documents/${generation.documentId}/appendices/preview`, {
    method: "POST",
    plan,
    body: {
      appendixType: 3,
      answers: {
        pet_type: "Кошка",
        pet_count: 1,
        pets_details: "Домашняя кошка, проживает в квартире постоянно.",
        pet_residence_rules: "Арендатор следит за чистотой и компенсирует возможный ущерб."
      }
    },
  });

  ensure(appendixPetPreview.isReady === true, "Pet appendix preview should be ready.");

  const appendixPet = await withRetry(() =>
    requestJson(`/documents/${generation.documentId}/appendices`, {
      method: "POST",
      plan,
      body: {
        appendixType: 3,
        answers: {
          pet_type: "Кошка",
          pet_count: 1,
          pets_details: "Домашняя кошка, проживает в квартире постоянно.",
          pet_residence_rules: "Арендатор следит за чистотой и компенсирует возможный ущерб."
        }
      },
    }),
  );

  const appendices = await requestJson(`/documents/${generation.documentId}/appendices`, { plan });
  const appendixDetails = await requestJson(`/appendices/${appendixPet.appendixId}`, { plan });
  const documentPage = await requestText(`${frontendBase}/documents/${generation.documentId}`);
  const appendixFlowPage = await requestText(`${frontendBase}/documents/${generation.documentId}/appendices/1`);
  const documentPdf = await requestBinary(`${apiBase}/documents/${generation.documentId}/pdf`);
  const appendixPdf = await requestBinary(`${apiBase}/appendices/${appendixAct.appendixId}/pdf`);

  ensure(appendices.length >= 2, "Premium appendices were not created.");
  ensure(appendixDetails.content.length > 80, "Premium appendix content looks too short.");
  ensure(documentPage.includes("Предпросмотр договора и приложений"), "Document page did not render expected title.");
  ensure(appendixFlowPage.includes("мини-опрос"), "Appendix flow page did not render expected content.");
  ensure(String.fromCharCode(...documentPdf.slice(0, 5)) === "%PDF-", "Document PDF endpoint did not return a PDF.");
  ensure(String.fromCharCode(...appendixPdf.slice(0, 5)) === "%PDF-", "Appendix PDF endpoint did not return a PDF.");

  return {
    draftId: draft.draftId,
    documentId: generation.documentId,
    title: document.title,
    guideType: guide.guideType,
    guideFallback: guide.guideType === 1,
    appendices: [appendixAct.title, appendixPet.title],
    askPreview: ask.answer.slice(0, 140),
  };
}

async function runFreeFlow() {
  const plan = "Free";
  const draft = await requestJson("/drafts", {
    method: "POST",
    plan,
    body: { documentType: 1 },
  });

  const answers = [
    ["landlord_type", "individual"],
    ["landlord_name", "Сидоров Алексей Николаевич"],
    ["landlord_passport_number", "40 11 111111"],
    ["landlord_passport_issued_by", "ОВД Центрального района"],
    ["landlord_passport_unit_code", "780-001"],
    ["landlord_passport_issue_date", "11.06.2017"],
    ["landlord_registration_address", "г. Санкт-Петербург, ул. Мирная, д. 8, кв. 3"],
    ["landlord_phone", "+7 921 000-00-01"],
    ["tenant_type", "individual"],
    ["tenant_name", "Кузнецова Мария Павловна"],
    ["tenant_passport_number", "40 22 222222"],
    ["tenant_passport_issued_by", "ОВД Адмиралтейского района"],
    ["tenant_passport_unit_code", "780-002"],
    ["tenant_passport_issue_date", "04.09.2019"],
    ["tenant_registration_address", "г. Санкт-Петербург, Невский проспект, д. 50, кв. 12"],
    ["tenant_phone", "+7 921 000-00-02"],
    ["agreement_city", "Санкт-Петербург"],
    ["agreement_date", "01.05.2026"],
    ["property_type", "apartment"],
    ["property_address", "г. Санкт-Петербург, Невский проспект, д. 100, кв. 55"],
    ["property_description", "Однокомнатная квартира в хорошем состоянии."],
    ["lease_start_date", "01.05.2026"],
    ["lease_end_date", "30.04.2027"],
    ["rent_amount", 62000],
    ["payment_due_day", 10],
    ["payment_method", "cash"],
  ];

  await fillDraft(draft.draftId, plan, answers);

  const validation = await requestJson(`/drafts/${draft.draftId}/validate`, {
    method: "POST",
    plan,
    body: {},
  });
  ensure(validation.isValid === true, "Free draft failed validation.");

  const generation = await requestJson(`/drafts/${draft.draftId}/generate`, {
    method: "POST",
    plan,
    body: {
      includeGuide: true,
      requestedAppendices: [],
    },
  });

  const document = await requestJson(`/documents/${generation.documentId}`, { plan });
  const guide = await requestJson(`/documents/${generation.documentId}/guide`, { plan });
  const scenario = await requestJson("/document-types/1/scenario", { plan });
  const documentPage = await requestText(`${frontendBase}/documents/${generation.documentId}`);

  ensure(!document.title.includes("?"), "Free document title contains question marks instead of text.");
  ensure(document.title.includes("Невский"), "Free document title did not preserve the address.");
  ensure(guide.guideType === 1, "Free guide should be standard.");
  ensure(!scenario.steps.some((step) => step.featureCode !== null), "Free scenario should not expose premium steps.");
  ensure(documentPage.includes("Предпросмотр договора и приложений"), "Free document page did not render expected title.");

  let appendixDenied = false;
  try {
    await requestJson(`/documents/${generation.documentId}/appendices`, {
      method: "POST",
      plan,
      body: { appendixType: 1 },
    });
  } catch {
    appendixDenied = true;
  }

  ensure(appendixDenied, "Free plan should not allow appendix creation.");

  return {
    draftId: draft.draftId,
    documentId: generation.documentId,
    title: document.title,
    guideType: guide.guideType,
    steps: scenario.steps.length,
  };
}

async function main() {
  const homePage = await requestText(frontendBase);
  ensure(
    homePage.includes("Rental agreement workspace"),
    "Home page did not render expected dashboard shell.",
  );

  const premium = await runPremiumFlow();
  const free = await runFreeFlow();

  console.log(
    JSON.stringify(
      {
        home: "ok",
        premium,
        free,
      },
      null,
      2,
    ),
  );
}

main().catch((error) => {
  console.error(error.stack || error.message || String(error));
  process.exitCode = 1;
});
