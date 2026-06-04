export type PlanHeader = "Free" | "Premium";

export enum SubscriptionPlan {
  Free = 1,
  Premium = 2,
}

export enum DocumentType {
  RentalAgreement = 1,
}

export enum DraftStatus {
  Draft = 1,
  InProgress = 2,
  ReadyForGeneration = 3,
  Generating = 4,
  Generated = 5,
  Failed = 6,
  Archived = 7,
}

export enum DocumentStatus {
  Draft = 1,
  Generating = 2,
  Generated = 3,
  Failed = 4,
  Archived = 5,
}

export enum AppendixStatus {
  Draft = 1,
  Generating = 2,
  Generated = 3,
  Failed = 4,
}

export enum AppendixType {
  HandoverAct = 1,
  InventoryList = 2,
  PetAddendum = 3,
  PaymentSchedule = 4,
  DepositAgreement = 5,
  HouseRules = 6,
}

export enum GuideType {
  Standard = 1,
  Personalized = 2,
}

export enum FeatureCode {
  ExtendedRentalSections = 1,
  PersonalizedGuide = 2,
  Appendices = 3,
}

export type StepValue = string | number | boolean | null;

export interface QuestionStepModel {
  section: string;
  stepKey: string;
  title: string;
  questionText: string;
  inputType: string;
  required: boolean;
  placeholder: string | null;
  helpText: string | null;
  options: string[];
  featureCode?: FeatureCode | null;
  value?: StepValue;
}

export interface DocumentTypeCard {
  type: DocumentType;
  title: string;
  supportsDialog: boolean;
}

export interface ScenarioStep {
  section: string;
  key: string;
  title: string;
  questionText: string;
  inputType: string;
  required: boolean;
  placeholder: string | null;
  helpText: string | null;
  mapsTo: string | null;
  featureCode: FeatureCode | null;
  dependsOn: Record<string, string> | null;
  options: string[];
}

export interface ScenarioDefinition {
  version: string;
  documentType: DocumentType;
  steps: ScenarioStep[];
}

export interface CreateDraftResponse {
  draftId: string;
  status: DraftStatus;
  documentType: DocumentType;
  scenarioVersion: string;
  currentStepKey: string | null;
}

export interface DraftDetailsResponse {
  draftId: string;
  documentType: DocumentType;
  status: DraftStatus;
  title: string;
  scenarioVersion: string;
  currentStepKey: string | null;
  completionPercent: number;
  answersJson: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface DraftStepItemResponse extends QuestionStepModel {
  isAnswered: boolean;
  isCurrent: boolean;
  featureCode: FeatureCode | null;
}

export type AppendixFlowStepResponse = QuestionStepModel;

export interface AppendixFlowResponse {
  documentId: string;
  appendixType: AppendixType;
  title: string;
  description: string;
  steps: AppendixFlowStepResponse[];
}

export interface AppendixPreviewResponse {
  documentId: string;
  appendixType: AppendixType;
  title: string;
  description: string;
  isReady: boolean;
  content: string;
  errors: ValidationIssue[];
  missingSteps: string[];
  normalizedAnswers: Record<string, StepValue>;
}

export interface NextQuestionResponse {
  draftId: string;
  section: string;
  stepKey: string;
  title: string;
  questionText: string;
  inputType: string;
  required: boolean;
  placeholder: string | null;
  helpText: string | null;
  options: string[];
  featureCode: FeatureCode | null;
  isAvailableForCurrentPlan: boolean;
}

export interface SaveAnswerResponse {
  draftId: string;
  status: DraftStatus;
  savedStepKey: string;
  nextStepKey: string | null;
  completionPercent: number;
}

export interface ValidationIssue {
  code: string;
  message: string;
  field?: string | null;
  stepKey?: string | null;
}

export interface ValidateDraftResponse {
  isValid: boolean;
  status: DraftStatus;
  completionPercent: number;
  errors: ValidationIssue[];
  missingSteps: string[];
}

export interface AskAiResponse {
  answer: string;
  relatedStepKey: string | null;
  disclaimer: string;
  source: "deepseek" | "fallback";
}

export interface GenerateDocumentResponse {
  documentId: string;
  status: DocumentStatus;
  guideMode: GuideType;
  generatedAtUtc: string;
  availableAppendices: AppendixType[];
}

export interface DocumentListItemResponse {
  documentId: string;
  title: string;
  documentType: DocumentType;
  status: DocumentStatus;
  createdAtUtc: string;
  hasGuide: boolean;
  appendicesCount: number;
}

export interface AppendixSummaryResponse {
  appendixId: string;
  appendixType: AppendixType;
  status: AppendixStatus;
  title: string;
}

export interface GuideResponse {
  documentId: string;
  guideType: GuideType;
  content: string;
}

export interface DocumentDetailsResponse {
  documentId: string;
  draftId: string | null;
  documentType: DocumentType;
  status: DocumentStatus;
  title: string;
  content: string;
  plan: SubscriptionPlan;
  generatedAtUtc: string | null;
  guide: GuideResponse | null;
  appendices: AppendixSummaryResponse[];
}

export interface AppendixDetailsResponse {
  appendixId: string;
  parentDocumentId: string;
  appendixType: AppendixType;
  status: AppendixStatus;
  title: string;
  content: string;
  generatedAtUtc: string | null;
}

export interface CurrentUserResponse {
  userId: string;
  email: string;
  plan: SubscriptionPlan;
}

export interface AuthResponse {
  userId: string;
  email: string;
  fullName?: string | null;
  plan: SubscriptionPlan;
  token: string;
}

export interface ApiErrorResponse {
  traceId?: string;
  code?: string;
  message?: string;
  details?: string;
  errors?: ValidationIssue[];
}
