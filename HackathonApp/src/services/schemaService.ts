import httpClient from "./httpClient";
import type { DatabaseOverview, TableInfo, ColumnInfo, DbObject } from "@/types";

export const schemaService = {
  getOverview: async (): Promise<DatabaseOverview> => {
    const res = await httpClient.get("/api/schema/overview");
    return res.data.data;
  },

  getTables: async (): Promise<TableInfo[]> => {
    const res = await httpClient.get("/api/schema/tables");
    return res.data.data;
  },

  getTableColumns: async (tableName: string): Promise<ColumnInfo[]> => {
    const res = await httpClient.get(`/api/schema/tables/${tableName}/columns`);
    return res.data.data;
  },

  getTableData: async (tableName: string, page = 1, pageSize = 25) => {
    const res = await httpClient.get(`/api/schema/tables/${tableName}/data`, { params: { page, pageSize } });
    return res.data.data;
  },

  getViews: async (): Promise<DbObject[]> => {
    const res = await httpClient.get("/api/schema/views");
    return res.data.data;
  },

  getViewDefinition: async (name: string) => {
    const res = await httpClient.get(`/api/schema/views/${name}/definition`);
    return res.data.data;
  },

  getProcedures: async (): Promise<DbObject[]> => {
    const res = await httpClient.get("/api/schema/procedures");
    return res.data.data;
  },

  getProcedureDefinition: async (name: string) => {
    const res = await httpClient.get(`/api/schema/procedures/${name}/definition`);
    return res.data.data;
  },

  getFunctions: async (): Promise<DbObject[]> => {
    const res = await httpClient.get("/api/schema/functions");
    return res.data.data;
  },

  getFunctionDefinition: async (name: string) => {
    const res = await httpClient.get(`/api/schema/functions/${name}/definition`);
    return res.data.data;
  },

  getTriggers: async (): Promise<DbObject[]> => {
    const res = await httpClient.get("/api/schema/triggers");
    return res.data.data;
  },

  getTriggerDefinition: async (name: string) => {
    const res = await httpClient.get(`/api/schema/triggers/${name}/definition`);
    return res.data.data;
  },
};
