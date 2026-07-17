import { useEffect, useState, useCallback } from "react";
import { Table, Eye, Cog, FunctionSquare, Zap, ChevronRight, ChevronDown, Columns, Loader2, RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { schemaService } from "@/services/schemaService";
import type { TableInfo, ColumnInfo, DbObject, DatabaseOverview } from "@/types";

interface SchemaExplorerProps {
  onLoadDefinition?: (name: string, definition: string) => void;
}

type NodeType = "tables" | "views" | "procedures" | "functions" | "triggers";

export default function SchemaExplorer({ onLoadDefinition }: SchemaExplorerProps) {
  const [overview, setOverview] = useState<DatabaseOverview | null>(null);
  const [expandedSections, setExpandedSections] = useState<Set<NodeType>>(new Set(["tables"]));
  const [tables, setTables] = useState<TableInfo[]>([]);
  const [views, setViews] = useState<DbObject[]>([]);
  const [procedures, setProcedures] = useState<DbObject[]>([]);
  const [functions, setFunctions] = useState<DbObject[]>([]);
  const [triggers, setTriggers] = useState<DbObject[]>([]);
  const [expandedTable, setExpandedTable] = useState<string | null>(null);
  const [tableColumns, setTableColumns] = useState<Record<string, ColumnInfo[]>>({});
  const [loading, setLoading] = useState(true);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const ov = await schemaService.getOverview();
      setOverview(ov);
      // Load tables by default
      const t = await schemaService.getTables();
      setTables(t);
    } catch (err: any) {
      toast.error("Failed to load schema");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  const toggleSection = async (section: NodeType) => {
    const next = new Set(expandedSections);
    if (next.has(section)) {
      next.delete(section);
    } else {
      next.add(section);
      // Lazy-load section data
      try {
        if (section === "views" && views.length === 0) setViews(await schemaService.getViews());
        if (section === "procedures" && procedures.length === 0) setProcedures(await schemaService.getProcedures());
        if (section === "functions" && functions.length === 0) setFunctions(await schemaService.getFunctions());
        if (section === "triggers" && triggers.length === 0) setTriggers(await schemaService.getTriggers());
      } catch { /* silent */ }
    }
    setExpandedSections(next);
  };

  const toggleTable = async (tableName: string) => {
    if (expandedTable === tableName) {
      setExpandedTable(null);
    } else {
      setExpandedTable(tableName);
      if (!tableColumns[tableName]) {
        try {
          const cols = await schemaService.getTableColumns(tableName);
          setTableColumns((prev) => ({ ...prev, [tableName]: cols }));
        } catch { /* silent */ }
      }
    }
  };

  const handleObjectClick = async (type: "views" | "procedures" | "functions" | "triggers", name: string) => {
    if (!onLoadDefinition) return;
    try {
      let result: { definition: string };
      switch (type) {
        case "views": result = await schemaService.getViewDefinition(name); break;
        case "procedures": result = await schemaService.getProcedureDefinition(name); break;
        case "functions": result = await schemaService.getFunctionDefinition(name); break;
        case "triggers": result = await schemaService.getTriggerDefinition(name); break;
      }
      if (result?.definition) {
        onLoadDefinition(name, result.definition);
      }
    } catch {
      toast.error("Could not load definition");
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="h-6 w-6 animate-spin text-teal-500" />
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col overflow-hidden text-sm">
      {/* Header */}
      <div className="px-3 py-2 border-b flex items-center justify-between shrink-0 bg-gray-50">
        <span className="font-semibold text-gray-700 text-xs uppercase tracking-wide">Schema Explorer</span>
        <button onClick={loadData} className="text-gray-400 hover:text-teal-600" title="Refresh">
          <RefreshCw className="h-3.5 w-3.5" />
        </button>
      </div>

      {/* Overview */}
      {/* Tree */}
      <div className="flex-1 overflow-y-auto px-1 py-1">
        {/* Tables */}
        <SectionNode
          label={`Tables (${tables.length})`}
          icon={<Table className="h-4 w-4 text-blue-500" />}
          expanded={expandedSections.has("tables")}
          onToggle={() => toggleSection("tables")}
        >
          {tables.map((t) => (
            <div key={t.tableName}>
              <button
                onClick={() => toggleTable(t.tableName)}
                className="flex items-center gap-1.5 w-full px-2 py-1 rounded hover:bg-gray-100 text-left group"
              >
                {expandedTable === t.tableName ? <ChevronDown className="h-3 w-3 text-gray-400" /> : <ChevronRight className="h-3 w-3 text-gray-400" />}
                <Table className="h-3.5 w-3.5 text-blue-400" />
                <span className="truncate text-gray-700">{t.tableName}</span>
                <span className="ml-auto text-xs text-gray-400 opacity-0 group-hover:opacity-100">{t.rowCount} rows</span>
              </button>
              {expandedTable === t.tableName && tableColumns[t.tableName] && (
                <div className="ml-6 border-l pl-2 mb-1">
                  {tableColumns[t.tableName].map((col) => (
                    <div key={col.columnName} className="flex items-center gap-1.5 px-1 py-0.5 text-xs text-gray-600">
                      <Columns className="h-3 w-3 text-gray-400" />
                      <span className={col.isPrimaryKey ? "font-semibold text-amber-700" : ""}>
                        {col.columnName}
                      </span>
                      <span className="text-gray-400 font-mono">{col.dataType}</span>
                      {col.isPrimaryKey && <span className="text-amber-500 text-[10px]">PK</span>}
                      {col.isForeignKey && <span className="text-purple-500 text-[10px]">FK</span>}
                      {!col.isNullable && <span className="text-red-400 text-[10px]">NN</span>}
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </SectionNode>

        {/* Views */}
        <SectionNode
          label={`Views (${overview?.viewCount ?? 0})`}
          icon={<Eye className="h-4 w-4 text-green-500" />}
          expanded={expandedSections.has("views")}
          onToggle={() => toggleSection("views")}
        >
          {views.map((v) => (
            <ObjectItem key={v.name} name={v.name} icon={<Eye className="h-3.5 w-3.5 text-green-400" />} onClick={() => handleObjectClick("views", v.name)} />
          ))}
        </SectionNode>

        {/* Procedures */}
        <SectionNode
          label={`Procedures (${overview?.procedureCount ?? 0})`}
          icon={<Cog className="h-4 w-4 text-purple-500" />}
          expanded={expandedSections.has("procedures")}
          onToggle={() => toggleSection("procedures")}
        >
          {procedures.map((p) => (
            <ObjectItem key={p.name} name={p.name} icon={<Cog className="h-3.5 w-3.5 text-purple-400" />} onClick={() => handleObjectClick("procedures", p.name)} />
          ))}
        </SectionNode>

        {/* Functions */}
        <SectionNode
          label={`Functions (${overview?.functionCount ?? 0})`}
          icon={<FunctionSquare className="h-4 w-4 text-cyan-500" />}
          expanded={expandedSections.has("functions")}
          onToggle={() => toggleSection("functions")}
        >
          {functions.map((f) => (
            <ObjectItem key={f.name} name={f.name} icon={<FunctionSquare className="h-3.5 w-3.5 text-cyan-400" />} onClick={() => handleObjectClick("functions", f.name)} />
          ))}
        </SectionNode>

        {/* Triggers */}
        <SectionNode
          label={`Triggers (${overview?.triggerCount ?? 0})`}
          icon={<Zap className="h-4 w-4 text-orange-500" />}
          expanded={expandedSections.has("triggers")}
          onToggle={() => toggleSection("triggers")}
        >
          {triggers.map((t) => (
            <ObjectItem key={t.name} name={t.name} icon={<Zap className="h-3.5 w-3.5 text-orange-400" />} onClick={() => handleObjectClick("triggers", t.name)} />
          ))}
        </SectionNode>
      </div>
    </div>
  );
}

function SectionNode({ label, icon, expanded, onToggle, children }: {
  label: string; icon: React.ReactNode; expanded: boolean; onToggle: () => void; children: React.ReactNode;
}) {
  return (
    <div className="mb-0.5">
      <button onClick={onToggle} className="flex items-center gap-2 w-full px-2 py-1.5 rounded-md hover:bg-gray-100 font-medium text-gray-700">
        {expanded ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
        {icon}
        <span>{label}</span>
      </button>
      {expanded && <div className="ml-3">{children}</div>}
    </div>
  );
}

function ObjectItem({ name, icon, onClick }: { name: string; icon: React.ReactNode; onClick: () => void }) {
  return (
    <button onClick={onClick} className="flex items-center gap-1.5 w-full px-2 py-1 rounded hover:bg-gray-100 text-left text-gray-700">
      {icon}
      <span className="truncate">{name}</span>
    </button>
  );
}
