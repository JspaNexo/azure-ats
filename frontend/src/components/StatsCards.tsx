import { Users, UserCheck, AlertCircle, FileCheck, CheckCircle2, Award } from 'lucide-react';
import { Candidate } from '../types';
import { useAuth } from '../context/AuthContext';

interface StatsCardsProps {
  candidates: Candidate[];
}

export const StatsCards = ({ candidates }: StatsCardsProps) => {
  const { isAdmin, user } = useAuth();

  const total = candidates.length;
  const unassignedCount = candidates.filter((c) => !c.assignedRecruiterId).length;
  const assignedCount = candidates.filter((c) => !!c.assignedRecruiterId).length;
  const evaluatedCount = candidates.filter((c) => c.evaluatorDecision && c.evaluatorDecision !== 'Pending').length;

  // Recruiter specific metrics
  const myCandidates = candidates.filter(
    (c) =>
      c.assignedRecruiterId === user?.username ||
      c.assignedRecruiterId === user?.id ||
      c.assignedRecruiterEmail === user?.email
  );
  const myEvaluated = myCandidates.filter((c) => c.evaluatorDecision && c.evaluatorDecision !== 'Pending').length;
  const myPending = myCandidates.length - myEvaluated;
  const myAvgMatch = myCandidates.length > 0
    ? Math.round(myCandidates.reduce((sum, c) => sum + (c.matchScore || 90), 0) / myCandidates.length)
    : 0;

  const adminStats = [
    {
      name: 'Total Postulantes',
      value: total,
      subtext: 'Expedientes activos',
      icon: Users,
      highlight: false,
    },
    {
      name: 'Sin Asignar',
      value: unassignedCount,
      subtext: unassignedCount > 0 ? 'Pendientes de delegar' : 'Todos asignados',
      icon: AlertCircle,
      highlight: unassignedCount > 0,
    },
    {
      name: 'Expedientes Asignados',
      value: assignedCount,
      subtext: 'En revisión o entrevista',
      icon: UserCheck,
      highlight: false,
    },
    {
      name: 'Dictámenes Emitidos',
      value: `${evaluatedCount} de ${total}`,
      subtext: total > 0 ? `${Math.round((evaluatedCount / total) * 100)}% de avance` : 'Sin postulantes',
      icon: FileCheck,
      highlight: false,
    },
  ];

  const recruiterStats = [
    {
      name: 'Mis Candidatos',
      value: myCandidates.length,
      subtext: 'Expedientes a tu cargo',
      icon: Users,
      highlight: false,
    },
    {
      name: 'Pendientes de Entrevista',
      value: myPending,
      subtext: myPending > 0 ? 'Por entrevistar' : 'Completado',
      icon: AlertCircle,
      highlight: myPending > 0,
    },
    {
      name: 'Entrevistas Concluidas',
      value: `${myEvaluated} de ${myCandidates.length}`,
      subtext: 'Resolución guardada',
      icon: CheckCircle2,
      highlight: false,
    },
    {
      name: 'Ajuste Técnico Promedio',
      value: myCandidates.length > 0 ? `${myAvgMatch}%` : 'N/A',
      subtext: 'Promedio de tu grupo',
      icon: Award,
      highlight: false,
    },
  ];

  const stats = isAdmin ? adminStats : recruiterStats;

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
      {stats.map((stat, i) => {
        const Icon = stat.icon;
        return (
          <div
            key={i}
            className={`p-3.5 sm:p-4 bg-white rounded-xl border transition-all ${
              stat.highlight
                ? 'border-amber-300 bg-amber-50/25 shadow-2xs'
                : 'border-slate-200/80 hover:border-slate-300 shadow-2xs'
            }`}
          >
            <div className="flex items-center justify-between">
              <p className="text-[11px] sm:text-xs font-bold text-slate-500 uppercase tracking-wider truncate pr-2" title={stat.name}>
                {stat.name}
              </p>
              <div className="p-1.5 rounded-lg bg-slate-50 text-slate-500 border border-slate-100 flex-shrink-0">
                <Icon className="w-4 h-4" />
              </div>
            </div>
            <p className="text-xl sm:text-2xl font-bold text-slate-900 mt-1 tracking-tight">{stat.value}</p>
            <p className="text-[11px] sm:text-xs text-slate-400 mt-0.5 truncate">{stat.subtext}</p>
          </div>
        );
      })}
    </div>
  );
};
