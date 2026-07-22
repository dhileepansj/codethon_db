import httpClient from "./httpClient";

export interface ManualTestWorkspace {
  assessmentId: number;
  title: string;
  useCaseHtml?: string;
  durationMinutes?: number;
  scenarios: ScenarioDto[];
}

export interface ScenarioDto {
  id: number;
  scenarioId: string;
  scenario?: string;
  description?: string;
  mustTest?: string;
  passFail?: string;
  sortOrder: number;
  testCaseCount: number;
}

export interface TestCaseDto {
  id: number;
  scenarioDbId: number;
  testCaseId: string;
  stepNo: string;
  inputSpecification?: string;
  helpRemarks?: string;
  inputTestData?: string;
  expectedResult?: string;
  actualResult?: string;
  stepResult?: string;
  sortOrder: number;
}

export const manualTestService = {
  getWorkspace: async (): Promise<ManualTestWorkspace> => {
    const res = await httpClient.get("/api/manual-test/workspace");
    return res.data.data;
  },

  getSubmissionStatus: async (): Promise<{ isSubmitted: boolean; submittedAt?: string }> => {
    const res = await httpClient.get("/api/manual-test/submission-status");
    return res.data.data;
  },

  submit: async () => {
    const res = await httpClient.post("/api/manual-test/submit");
    return res.data;
  },

  saveScenario: async (data: Partial<ScenarioDto> & { id?: number }) => {
    const res = await httpClient.post("/api/manual-test/scenarios", data);
    return res.data.data ?? res.data;
  },

  deleteScenario: async (id: number) => {
    const res = await httpClient.delete(`/api/manual-test/scenarios/${id}`);
    return res.data;
  },

  getTestCases: async (scenarioId: number): Promise<TestCaseDto[]> => {
    const res = await httpClient.get(`/api/manual-test/scenarios/${scenarioId}/cases`);
    return res.data.data;
  },

  saveTestCase: async (data: any) => {
    const res = await httpClient.post("/api/manual-test/cases", data);
    return res.data.data ?? res.data;
  },

  deleteTestCase: async (id: number) => {
    const res = await httpClient.delete(`/api/manual-test/cases/${id}`);
    return res.data;
  },
};
