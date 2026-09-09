import { useState, useMemo } from 'react';
import {
  Search,
  X,
  ChevronRight,
  Filter,
  CheckCircle2,
  SlidersHorizontal,
  UserCheck,
  RotateCcw,
  SearchX,
  FileText
} from 'lucide-react';
import { Candidate } from '../types';
import { useAuth } from '../context/AuthContext';
import { PdfViewerModal } from './PdfViewerModal';

interface EvaluatorCandidateDashboardProps {
  candidates: Candidate[];
  onSelectCandidate: (candidate: Candidate) => void;
  onAssignCandidate?: (candidate: Candidate) => void;
}

// Generate clean initials from candidate name
const getInitials = (firstName: string, lastName: string) => {
  const f = firstName?.trim().charAt(0) || '';
  const l = lastName?.trim().charAt(0) || '';
  return `${f}${l}`.toUpperCase() || 'CV';
};

export const EvaluatorCandidateDashboard = ({
  candidates,
  onSelectCandidate,
  onAssignCandidate,
}: EvaluatorCandidateDashboardProps) => {
  const { user, isAdmin, isRecruiter } = useAuth();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>('ALL');
  const [selectedDecision, setSelectedDecision] = useState<string>('ALL');
  const [activeTab, setActiveTab] = useState<string>(isRecruiter ? 'MINE' : 'ALL');
  const [sortBy, setSortBy] = useState<'match' | 'recent'>('match');
  const [selectedPdfCandidate, setSelectedPdfCandidate] = useState<Candidate | null>(null);

  // Extract unique target roles
  const uniqueRoles = useMemo(() => {
    const roles = Array.from(new Set(candidates.map((c) => c.targetRole || 'General')));
    return roles.sort();
  }, [candidates]);

  // Recruiter assignments count
  const myCandidatesCount = useMemo(() => {
    return candidates.filter(
      (c) =>
        c.assignedRecruiterId === user?.username ||
        c.assignedRecruiterId === user?.id ||
        c.assignedRecruiterEmail === user?.email
    ).length;
  }, [candidates, user]);

  const unassignedCount = useMemo(() => {
    return candidates.filter((c) => !c.assignedRecruiterId).length;
  }, [candidates]);

  const assignedCount = useMemo(() => {
    return candidates.filter((c) => !!c.assignedRecruiterId).length;
  }, [candidates]);

  const evaluatedCount = useMemo(() => {
    return candidates.filter((c) => c.evaluatorDecision && c.evaluatorDecision !== 'Pending').length;
  }, [candidates]);

  // Filter & Sort candidates
  const filteredAndSortedCandidates = useMemo(() => {
    return candidates
      .filter((c) => {
        // Tab filtering
        let matchesTab = true;
        if (activeTab === 'MINE') {
          matchesTab =
            c.assignedRecruiterId === user?.username ||
            c.assignedRecruiterId === user?.id ||
            c.assignedRecruiterEmail === user?.email;
        } else if (activeTab === 'UNASSIGNED') {
          matchesTab = !c.assignedRecruiterId;
        } else if (activeTab === 'ASSIGNED') {
          matchesTab = !!c.assignedRecruiterId;
        } else if (activeTab === 'EVALUATED') {
          matchesTab = !!c.evaluatorDecision && c.evaluatorDecision !== 'Pending';
        }

        // Secondary dropdown filters
        const matchesRole = selectedRole === 'ALL' || (c.targetRole || 'General') === selectedRole;
        const candidateDecision = c.evaluatorDecision || 'Pending';
        const matchesDecision = selectedDecision === 'ALL' || candidateDecision === selectedDecision;

        // Search text matching
        const searchLower = searchTerm.toLowerCase().trim();
        const matchesSearch =
          !searchLower ||
          c.firstName.toLowerCase().includes(searchLower) ||
          c.lastName.toLowerCase().includes(searchLower) ||
          c.email.toLowerCase().includes(searchLower) ||
          (c.targetRole || '').toLowerCase().includes(searchLower) ||
          (c.assignedRecruiterName || '').toLowerCase().includes(searchLower) ||
          (c.cvAnalysis?.skills.some((s) => (s.normalizedName || s.name).toLowerCase().includes(searchLower)) ?? false);

        return matchesTab && matchesRole && matchesDecision && matchesSearch;
      })
      .sort((a, b) => {
        if (sortBy === 'match') {
          return (b.matchScore || 0) - (a.matchScore || 0);
        }
        return new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
      });
  }, [candidates, activeTab, selectedRole, selectedDecision, searchTerm, sortBy, user]);

  // Group by Target Role
  const groupedByRole = useMemo(() => {
    const groups: { [role: string]: Candidate[] } = {};
    for (const c of filteredAndSortedCandidates) {
      const role = c.targetRole || 'Puesto General';
      if (!groups[role]) groups[role] = [];
      groups[role].push(c);
    }
    return groups;
  }, [filteredAndSortedCandidates]);

  const hasActiveFilters =
    searchTerm !== '' ||
    selectedRole !== 'ALL' ||
    selectedDecision !== 'ALL' ||
    (isAdmin ? activeTab !== 'ALL' : activeTab !== 'MINE');

  const handleResetFilters = () => {
    setSearchTerm('');
    setSelectedRole('ALL');
    setSelectedDecision('ALL');
    setActiveTab(isRecruiter ? 'MINE' : 'ALL');
  };

  return (
    <div className="space-y-6">
      {/* Search, Views & Filters Control Bar */}
      <div className="bg-white rounded-xl shadow-xs border border-slate-200/80 p-3.5 sm:p-4 space-y-3.5">
        {/* Top Row: Search & Sort */}
        <div className="flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-3">
          {/* Search Input */}
          <div className="relative flex-1">
            <Search className="absolute left-3.5 top-3 w-4 h-4 text-slate-400" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar candidato por nombre, cargo, email o tecnología..."
              className="w-full pl-10 pr-8 py-2.5 text-xs sm:text-sm bg-slate-50/70 border border-slate-200 rounded-lg focus:outline-none focus:ring-1 focus:ring-slate-400 focus:border-slate-400 focus:bg-white text-slate-800 placeholder:text-slate-400 transition-all"
            />
            {searchTerm && (
              <button
                type="button"
                onClick={() => setSearchTerm('')}
                className="absolute right-2.5 top-3 text-slate-400 hover:text-slate-600 p-0.5 rounded-full cursor-pointer"
                title="Limpiar búsqueda"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          {/* Role and Decision Dropdowns & Sort */}
          <div className="grid grid-cols-2 sm:flex items-center gap-2">
            <div className="relative min-w-0 sm:min-w-[170px] col-span-1">
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="w-full py-2.5 pl-3 pr-8 text-xs sm:text-sm bg-slate-50/70 border border-slate-200 rounded-lg text-slate-700 focus:outline-none focus:border-slate-400 appearance-none cursor-pointer truncate font-medium"
              >
                <option value="ALL">Todas las Posiciones</option>
                {uniqueRoles.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
              <Filter className="w-3.5 h-3.5 text-slate-400 absolute right-3 top-3.5 pointer-events-none" />
            </div>

            <div className="relative min-w-0 sm:min-w-[155px] col-span-1">
              <select
                value={selectedDecision}
                onChange={(e) => setSelectedDecision(e.target.value)}
                className="w-full py-2.5 pl-3 pr-8 text-xs sm:text-sm bg-slate-50/70 border border-slate-200 rounded-lg text-slate-700 focus:outline-none focus:border-slate-400 appearance-none cursor-pointer truncate font-medium"
              >
                <option value="ALL">Todos los Estados</option>
                <option value="Approved">Aprobados</option>
                <option value="Waitlisted">En Reserva</option>
                <option value="Rejected">No Seleccionados</option>
                <option value="Pending">Pendientes</option>
              </select>
              <CheckCircle2 className="w-3.5 h-3.5 text-slate-400 absolute right-3 top-3.5 pointer-events-none" />
            </div>

            {/* Sort Toggle */}
            <div className="flex items-center space-x-1 sm:pl-2 sm:border-l sm:border-slate-200 text-xs text-slate-500 col-span-2 justify-end sm:justify-start">
              <SlidersHorizontal className="w-3.5 h-3.5 text-slate-400 mr-1 flex-shrink-0" />
              <button
                onClick={() => setSortBy('match')}
                className={`px-2.5 py-1.5 rounded text-xs font-medium transition-colors cursor-pointer ${
                  sortBy === 'match' ? 'bg-slate-100 text-slate-900 font-semibold' : 'text-slate-500 hover:text-slate-700'
                }`}
              >
                % Match
              </button>
              <button
                onClick={() => setSortBy('recent')}
                className={`px-2.5 py-1.5 rounded text-xs font-medium transition-colors cursor-pointer ${
                  sortBy === 'recent' ? 'bg-slate-100 text-slate-900 font-semibold' : 'text-slate-500 hover:text-slate-700'
                }`}
              >
                Recientes
              </button>
            </div>
          </div>
        </div>

        {/* Bottom Row: Segmented View Tabs */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between pt-2.5 border-t border-slate-100 gap-2 text-sm">
          <div className="flex items-center space-x-1.5 overflow-x-auto scrollbar-none py-0.5 w-full sm:w-auto -mx-1 px-1">
            {isAdmin ? (
              <>
                <button
                  type="button"
                  onClick={() => setActiveTab('ALL')}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium whitespace-nowrap flex-shrink-0 transition-all cursor-pointer ${
                    activeTab === 'ALL'
                      ? 'bg-slate-900 text-white shadow-2xs font-semibold'
                      : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  Todos ({candidates.length})
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('UNASSIGNED')}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium whitespace-nowrap flex-shrink-0 transition-all flex items-center space-x-1.5 cursor-pointer ${
                    activeTab === 'UNASSIGNED'
                      ? 'bg-amber-600 text-white shadow-2xs font-semibold'
                      : unassignedCount > 0
                      ? 'text-amber-800 bg-amber-50 hover:bg-amber-100 font-semibold'
                      : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  <span>Sin Asignar ({unassignedCount})</span>
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('ASSIGNED')}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium whitespace-nowrap flex-shrink-0 transition-all cursor-pointer ${
                    activeTab === 'ASSIGNED'
                      ? 'bg-slate-900 text-white shadow-2xs font-semibold'
                      : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  Asignados ({assignedCount})
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('EVALUATED')}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium whitespace-nowrap flex-shrink-0 transition-all cursor-pointer ${
                    activeTab === 'EVALUATED'
                      ? 'bg-slate-900 text-white shadow-2xs font-semibold'
                      : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  Evaluados ({evaluatedCount})
                </button>
              </>
            ) : (
              <>
                <button
                  type="button"
                  onClick={() => setActiveTab('MINE')}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium whitespace-nowrap flex-shrink-0 transition-all cursor-pointer ${
                    activeTab === 'MINE'
                      ? 'bg-slate-900 text-white shadow-2xs font-semibold'
                      : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  Mis Asignados ({myCandidatesCount})
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('ALL')}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs sm:text-sm font-medium whitespace-nowrap flex-shrink-0 transition-all cursor-pointer ${
                    activeTab === 'ALL'
                      ? 'bg-slate-900 text-white shadow-2xs font-semibold'
                      : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  Banco Completo ({candidates.length})
                </button>
              </>
            )}
          </div>

          {/* Active Filter Reset */}
          {hasActiveFilters && (
            <button
              type="button"
              onClick={handleResetFilters}
              className="text-xs sm:text-sm text-slate-500 hover:text-slate-800 flex items-center space-x-1.5 transition-colors cursor-pointer self-end sm:self-auto py-1"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              <span>Limpiar filtros</span>
            </button>
          )}
        </div>
      </div>

      {/* Candidates List Grouped by Position */}
      {filteredAndSortedCandidates.length === 0 ? (
        /* Empty State */
        <div className="p-8 sm:p-10 text-center bg-white rounded-xl border border-slate-200/80 shadow-xs space-y-3">
          <div className="w-10 h-10 bg-slate-100 text-slate-400 rounded-xl flex items-center justify-center mx-auto">
            <SearchX className="w-5 h-5" />
          </div>
          <div className="space-y-1">
            <h4 className="text-base font-semibold text-slate-800">
              No se encontraron candidatos
            </h4>
            <p className="text-sm text-slate-500">
              Prueba modificando los filtros de búsqueda o ingresando nuevos términos.
            </p>
          </div>
          <button
            type="button"
            onClick={handleResetFilters}
            className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-medium rounded-lg transition-colors inline-flex items-center space-x-1.5 cursor-pointer"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span>Restablecer</span>
          </button>
        </div>
      ) : (
        /* Calm, Grouped Candidates */
        Object.entries(groupedByRole).map(([role, list]) => (
          <div key={role} className="space-y-2.5">
            {/* Minimalist Section Header */}
            <div className="flex items-center justify-between px-1 pt-2 pb-1 border-b border-slate-200/80">
              <div className="flex items-center space-x-2">
                <span className="w-2 h-2 rounded-full bg-slate-400"></span>
                <h3 className="text-xs sm:text-sm font-bold text-slate-800 uppercase tracking-wider truncate">
                  {role}
                </h3>
                <span className="text-xs text-slate-400 font-medium whitespace-nowrap">
                  · {list.length} {list.length === 1 ? 'candidato' : 'candidatos'}
                </span>
              </div>
            </div>

            {/* Candidate Cards Grid / Rows */}
            <div className="space-y-2.5">
              {list.map((c) => {
                const initials = getInitials(c.firstName, c.lastName);

                return (
                  <div
                    key={c.id}
                    className="p-3.5 sm:p-5 bg-white hover:bg-slate-50/70 rounded-xl border border-slate-200/80 hover:border-slate-300 shadow-2xs transition-all flex flex-col md:flex-row md:items-center justify-between gap-3.5 sm:gap-4 group"
                  >
                    {/* Candidate Identity */}
                    <div className="flex items-start sm:items-center space-x-3 sm:space-x-3.5 min-w-0 flex-1">
                      {/* Calm Initials Avatar */}
                      <div className="w-10 h-10 sm:w-11 sm:h-11 rounded-xl bg-slate-100 text-slate-700 border border-slate-200 font-bold text-xs sm:text-sm flex items-center justify-center flex-shrink-0 mt-0.5 sm:mt-0">
                        {initials}
                      </div>

                      <div className="min-w-0 space-y-1 flex-1">
                        <div className="flex flex-wrap items-center gap-x-2.5 gap-y-1">
                          <button
                            type="button"
                            onClick={() => onSelectCandidate(c)}
                            className="text-sm sm:text-base font-bold text-slate-900 hover:text-blue-600 transition-colors truncate text-left cursor-pointer"
                          >
                            {c.firstName} {c.lastName}
                          </button>

                          {/* Cotejo de Requisitos */}
                          <span
                            className="text-[11px] sm:text-xs font-bold text-emerald-800 bg-emerald-50 px-2 sm:px-2.5 py-0.5 rounded-md flex-shrink-0 border border-emerald-200"
                            title="Cotejo informativo de tecnologías y experiencia detectadas en el CV frente a los requisitos del cargo. No constituye una evaluación ni calificación."
                          >
                            Cotejo: {c.matchScore ?? 0}% requisitos
                          </span>
                        </div>

                        {/* Metadata Line */}
                        <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs sm:text-sm text-slate-500">
                          <span className="font-medium text-slate-700">{c.seniority}</span>
                          <span>·</span>
                          <span>{c.experienceYears} años</span>
                          <span>·</span>
                          <span className="text-slate-400 truncate max-w-[180px] sm:max-w-[240px]">{c.email}</span>

                          {/* Assigned recruiter badge (visible on mobile too!) */}
                          {c.assignedRecruiterName && (
                            <>
                              <span className="hidden xs:inline">·</span>
                              <span className="inline-flex items-center space-x-1 text-slate-600 text-[11px] sm:text-xs font-medium bg-slate-100 px-2 py-0.5 rounded border border-slate-200" title={c.assignedRecruiterEmail || ''}>
                                <UserCheck className="w-3 h-3 text-slate-400" />
                                <span className="truncate max-w-[120px]">{c.assignedRecruiterName}</span>
                              </span>
                            </>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Right Column: Status, Assignment Action & Dossier Button */}
                    <div className="flex items-center justify-between md:justify-end space-x-3 pt-2 md:pt-0 border-t md:border-t-0 border-slate-100 text-xs sm:text-sm flex-shrink-0">
                      {/* Evaluation Decision */}
                      <div>
                        {c.evaluatorDecision === 'Approved' && (
                          <span className="inline-flex items-center space-x-1.5 text-emerald-700 font-semibold text-xs sm:text-sm">
                            <span className="w-2 h-2 rounded-full bg-emerald-600"></span>
                            <span>Aprobado</span>
                          </span>
                        )}
                        {c.evaluatorDecision === 'Waitlisted' && (
                          <span className="inline-flex items-center space-x-1.5 text-amber-700 font-semibold text-xs sm:text-sm">
                            <span className="w-2 h-2 rounded-full bg-amber-600"></span>
                            <span>En reserva</span>
                          </span>
                        )}
                        {c.evaluatorDecision === 'Rejected' && (
                          <span className="inline-flex items-center space-x-1.5 text-slate-500 font-medium text-xs sm:text-sm">
                            <span className="w-2 h-2 rounded-full bg-slate-400"></span>
                            <span>Descartado</span>
                          </span>
                        )}
                        {(!c.evaluatorDecision || c.evaluatorDecision === 'Pending') && (
                          <span className="inline-flex items-center space-x-1.5 text-slate-400 text-xs sm:text-sm">
                            <span className="w-2 h-2 rounded-full bg-slate-300"></span>
                            <span>Pendiente</span>
                          </span>
                        )}
                      </div>

                      {/* Recruiter Quick Assign / Reassign Button */}
                      <div className="flex items-center space-x-2">
                        {isAdmin && onAssignCandidate && (
                          <button
                            type="button"
                            onClick={() => onAssignCandidate(c)}
                            className={`p-2 rounded-lg transition-colors cursor-pointer text-xs font-semibold flex items-center space-x-1 ${
                              c.assignedRecruiterName
                                ? 'text-slate-400 hover:text-slate-700 hover:bg-slate-100'
                                : 'text-amber-700 hover:text-amber-900 bg-amber-50 hover:bg-amber-100 border border-amber-200/80 px-2.5'
                            }`}
                            title={c.assignedRecruiterName ? `Reasignar (Actual: ${c.assignedRecruiterName})` : 'Asignar a un evaluador'}
                          >
                            <UserCheck className="w-3.5 h-3.5" />
                            {!c.assignedRecruiterName && <span>Asignar</span>}
                          </button>
                        )}

                        {/* Quick Original CV Viewer Button */}
                        <button
                          type="button"
                          onClick={() => setSelectedPdfCandidate(c)}
                          className="px-2.5 py-1.5 sm:py-2 text-slate-700 hover:text-blue-700 hover:bg-blue-50 border border-slate-200 hover:border-blue-200 rounded-lg text-xs sm:text-sm font-semibold transition-all flex items-center space-x-1.5 cursor-pointer flex-shrink-0"
                          title="Visualizar currículum original (PDF) en la web sin descargarlo"
                        >
                          <FileText className="w-3.5 h-3.5 text-blue-600" />
                          <span className="hidden sm:inline">Ver CV</span>
                        </button>

                        {/* Dossier Review Button */}
                        <button
                          type="button"
                          onClick={() => onSelectCandidate(c)}
                          className="px-3 sm:px-3.5 py-1.5 sm:py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-semibold rounded-lg transition-all flex items-center space-x-1.5 cursor-pointer flex-shrink-0"
                        >
                          <span>Expediente</span>
                          <ChevronRight className="w-3.5 h-3.5 text-slate-300" />
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))
      )}

      {/* Visor Web Directo de CV Original */}
      <PdfViewerModal
        isOpen={!!selectedPdfCandidate}
        candidateId={selectedPdfCandidate?.id || null}
        candidateName={selectedPdfCandidate ? `${selectedPdfCandidate.firstName} ${selectedPdfCandidate.lastName}` : ''}
        onClose={() => setSelectedPdfCandidate(null)}
      />
    </div>
  );
};
