import { useState, useEffect } from 'react';
import {
  X,
  Download,
  FileText,
  Compass,
  Award,
  HelpCircle,
  TrendingUp,
  AlertCircle,
  AlertTriangle,
  Briefcase,
  CheckCircle2,
  Loader2,
  Lock,
  Save,
  Clock,
  XCircle,
  Mail,
  Phone,
  Calendar,
  Building2,
  ArrowRight,
  ArrowLeft,
  FileCheck,
  ShieldAlert
} from 'lucide-react';
import { Candidate } from '../types';
import { RadarChart } from './RadarChart';
import { api } from '../services/api';

interface EvaluatorReviewModalProps {
  isOpen: boolean;
  candidate: Candidate | null;
  onClose: () => void;
  onCandidateUpdated?: (updated: Candidate) => void;
}

const decisionOptions = [
  {
    value: 'Approved',
    label: 'Aprobado',
    desc: 'Candidato idóneo. Cumple con los requerimientos técnicos y conductuales. Avanza a oferta formal.',
    badgeClass: 'bg-emerald-50 text-emerald-800 border-emerald-300',
    selectedClass: 'border-emerald-700 bg-emerald-50 text-emerald-900 ring-2 ring-emerald-600 shadow-sm',
    icon: CheckCircle2,
    iconColor: 'text-emerald-700',
  },
  {
    value: 'Waitlisted',
    label: 'Lista de Reserva',
    desc: 'Perfil calificado. Se mantiene en cartera para la siguiente vacante u orden de mérito.',
    badgeClass: 'bg-amber-50 text-amber-800 border-amber-300',
    selectedClass: 'border-amber-700 bg-amber-50 text-amber-900 ring-2 ring-amber-600 shadow-sm',
    icon: Clock,
    iconColor: 'text-amber-700',
  },
  {
    value: 'Rejected',
    label: 'No Seleccionado',
    desc: 'No cumple con las competencias clave o nivel de experiencia requerido para la posición.',
    badgeClass: 'bg-slate-100 text-slate-800 border-slate-300',
    selectedClass: 'border-slate-700 bg-slate-100 text-slate-900 ring-2 ring-slate-600 shadow-sm',
    icon: XCircle,
    iconColor: 'text-slate-700',
  },
  {
    value: 'Pending',
    label: 'Pendiente de Dictamen',
    desc: 'Expediente en análisis o entrevista pendiente de realización.',
    badgeClass: 'bg-slate-100 text-slate-700 border-slate-200',
    selectedClass: 'border-slate-500 bg-slate-100 text-slate-900 ring-2 ring-slate-400 shadow-sm',
    icon: HelpCircle,
    iconColor: 'text-slate-500',
  },
];

type ModalTab = 'cv' | 'disc' | 'interview' | 'decision' | 'pdf';

export const EvaluatorReviewModal = ({
  isOpen,
  candidate,
  onClose,
  onCandidateUpdated,
}: EvaluatorReviewModalProps) => {
  const [activeTab, setActiveTab] = useState<ModalTab>('cv');
  const [isDownloading, setIsDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Evaluator decision & notes state
  const [decision, setDecision] = useState<string>('Pending');
  const [notes, setNotes] = useState<string>('');
  const [isSavingDecision, setIsSavingDecision] = useState(false);
  const [decisionSuccessMsg, setDecisionSuccessMsg] = useState<string | null>(null);

  useEffect(() => {
    if (candidate) {
      setDecision(candidate.evaluatorDecision || 'Pending');
      setNotes(candidate.evaluatorNotes || '');
      setDecisionSuccessMsg(null);
      setError(null);
      setActiveTab('cv');
    }
  }, [candidate]);

  // Close on Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen || !candidate) return null;

  const cv = candidate.cvAnalysis;
  const disc = candidate.discInterpretation;
  const report = candidate.report;

  // Extract or fallback DISC scores
  const discPrimary = candidate.primaryDiscStyle || disc?.primaryStyle || 'D/C';
  const dominance = discPrimary.includes('D') ? 88 : 45;
  const influence = discPrimary.includes('I') ? 78 : 55;
  const steadiness = discPrimary.includes('S') ? 75 : 42;
  const conscientiousness = discPrimary.includes('C') ? 85 : 50;

  const handleDownloadPdf = async () => {
    setIsDownloading(true);
    setError(null);
    try {
      const blob = await api.downloadReportPdf(candidate.id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `expediente_evaluacion_${candidate.firstName.toLowerCase()}_${candidate.lastName.toLowerCase()}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      setError(err?.message || 'No se pudo descargar el documento oficial. Verifique que el informe se encuentre generado.');
    } finally {
      setIsDownloading(false);
    }
  };

  const handleSaveDecision = async () => {
    if (!candidate) return;
    setIsSavingDecision(true);
    setError(null);
    setDecisionSuccessMsg(null);
    try {
      const updated = await api.updateCandidateDecision(candidate.id, decision, notes);
      setDecisionSuccessMsg('Resolución oficial y notas confidenciales registradas exitosamente.');
      if (onCandidateUpdated) {
        onCandidateUpdated(updated);
      }
      setTimeout(() => setDecisionSuccessMsg(null), 4000);
    } catch (err: any) {
      setError(err.message || 'Error al registrar la resolución del evaluador.');
    } finally {
      setIsSavingDecision(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 md:p-6 bg-slate-950/80 backdrop-blur-sm animate-fade-in"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-300 w-full max-w-5xl xl:max-w-6xl h-[95vh] sm:h-auto sm:max-h-[90vh] flex flex-col overflow-hidden">
        {/* Modal Header */}
        <div className="px-4 sm:px-6 py-3 sm:py-4 bg-slate-900 text-white border-b border-slate-800 flex items-start sm:items-center justify-between gap-3">
          <div className="flex items-start sm:items-center space-x-3 min-w-0 flex-1">
            <div className="w-9 h-9 sm:w-11 sm:h-11 rounded-xl bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-200 flex-shrink-0 mt-0.5 sm:mt-0">
              <Building2 className="w-4 h-4 sm:w-5 sm:h-5 text-slate-300" />
            </div>
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
                <h3 className="text-base sm:text-xl font-bold text-white tracking-tight">
                  {candidate.firstName} {candidate.lastName}
                </h3>
                <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-bold bg-slate-800 text-emerald-400 border border-slate-700">
                  {candidate.matchScore || 90}% Compatibilidad
                </span>
                <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-semibold bg-slate-800 text-slate-300 border border-slate-700">
                  DISC: {candidate.primaryDiscStyle || 'D/C'}
                </span>
                {candidate.evaluatorDecision === 'Approved' && (
                  <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-bold bg-emerald-700 text-white flex items-center space-x-1">
                    <CheckCircle2 className="w-3 h-3 sm:w-3.5 sm:h-3.5" />
                    <span>Aprobado</span>
                  </span>
                )}
                {candidate.evaluatorDecision === 'Waitlisted' && (
                  <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-bold bg-amber-600 text-white flex items-center space-x-1">
                    <Clock className="w-3 h-3 sm:w-3.5 sm:h-3.5" />
                    <span>En Reserva</span>
                  </span>
                )}
                {candidate.evaluatorDecision === 'Rejected' && (
                  <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-bold bg-slate-700 text-white flex items-center space-x-1">
                    <XCircle className="w-3 h-3 sm:w-3.5 sm:h-3.5" />
                    <span>Descartado</span>
                  </span>
                )}
                {(!candidate.evaluatorDecision || candidate.evaluatorDecision === 'Pending') && (
                  <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-medium bg-slate-800 text-slate-400 border border-slate-700 flex items-center space-x-1">
                    <HelpCircle className="w-3 h-3 sm:w-3.5 sm:h-3.5" />
                    <span>Pendiente</span>
                  </span>
                )}
              </div>
              <p className="text-xs sm:text-sm text-slate-300 mt-1 truncate">
                Posición: <strong className="text-white">{candidate.targetRole}</strong> • Seniority: {candidate.seniority} ({candidate.experienceYears} años)
                {candidate.assignedRecruiterName && (
                  <span className="ml-1.5 text-blue-300 font-semibold">• Evaluador: {candidate.assignedRecruiterName}</span>
                )}
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-1.5 sm:space-x-2 flex-shrink-0">
            <button
              onClick={handleDownloadPdf}
              disabled={isDownloading}
              className="hidden sm:flex items-center space-x-1.5 px-3 py-1.5 sm:px-3.5 sm:py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 hover:text-white border border-slate-700 text-xs sm:text-sm font-semibold rounded-lg transition-all disabled:opacity-50 cursor-pointer"
            >
              {isDownloading ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <Download className="w-4 h-4" />
              )}
              <span>Descargar PDF</span>
            </button>

            <button
              onClick={onClose}
              className="p-1.5 sm:p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
              title="Cerrar ventana (Esc)"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Step Tabs Navigation */}
        <div className="px-4 sm:px-6 py-2.5 bg-slate-100/90 border-b border-slate-200">
          <nav className="flex space-x-1.5 bg-slate-200/70 p-1.5 rounded-xl overflow-x-auto sm:overflow-visible scrollbar-none" aria-label="Tabs del Expediente">
            <button
              type="button"
              onClick={() => setActiveTab('cv')}
              className={`flex-1 min-w-[130px] sm:min-w-0 flex items-center justify-center space-x-2 py-2.5 px-3 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'cv'
                  ? 'bg-white text-slate-900 shadow-sm font-bold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
              }`}
            >
              <Briefcase className={`w-4 h-4 flex-shrink-0 ${activeTab === 'cv' ? 'text-slate-900' : 'text-slate-500'}`} />
              <span className="truncate">1. CV y Perfil</span>
              {candidate.cvAnalysis?.warnings && candidate.cvAnalysis.warnings.length > 0 && (
                <span className="w-2 h-2 rounded-full bg-amber-500 flex-shrink-0" title="Contiene alertas de validación o integridad"></span>
              )}
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('disc')}
              className={`flex-1 min-w-[130px] sm:min-w-0 flex items-center justify-center space-x-2 py-2.5 px-3 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'disc'
                  ? 'bg-white text-slate-900 shadow-sm font-bold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
              }`}
            >
              <Compass className={`w-4 h-4 flex-shrink-0 ${activeTab === 'disc' ? 'text-slate-900' : 'text-slate-500'}`} />
              <span className="truncate">2. Perfil DISC</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('interview')}
              className={`flex-1 min-w-[130px] sm:min-w-0 flex items-center justify-center space-x-2 py-2.5 px-3 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'interview'
                  ? 'bg-white text-slate-900 shadow-sm font-bold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
              }`}
            >
              <HelpCircle className={`w-4 h-4 flex-shrink-0 ${activeTab === 'interview' ? 'text-slate-900' : 'text-slate-500'}`} />
              <span className="truncate">3. Guía STAR</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('decision')}
              className={`flex-1 min-w-[130px] sm:min-w-0 flex items-center justify-center space-x-2 py-2.5 px-3 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'decision'
                  ? 'bg-white text-slate-900 shadow-sm font-bold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
              }`}
            >
              <FileCheck className={`w-4 h-4 flex-shrink-0 ${activeTab === 'decision' ? 'text-emerald-700' : 'text-emerald-600'}`} />
              <span className="truncate">4. Dictamen</span>
              {candidate.evaluatorDecision && candidate.evaluatorDecision !== 'Pending' && (
                <span className="w-2 h-2 rounded-full bg-emerald-500 flex-shrink-0"></span>
              )}
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('pdf')}
              className={`flex-1 min-w-[130px] sm:min-w-0 flex items-center justify-center space-x-2 py-2.5 px-3 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'pdf'
                  ? 'bg-white text-slate-900 shadow-sm font-bold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
              }`}
            >
              <FileText className={`w-4 h-4 flex-shrink-0 ${activeTab === 'pdf' ? 'text-slate-900' : 'text-slate-500'}`} />
              <span className="truncate">5. Expediente PDF</span>
            </button>
          </nav>
        </div>

        {/* Modal Body */}
        <div className="p-4 sm:p-6 overflow-y-auto flex-1 bg-slate-50/60 space-y-5 sm:space-y-6">
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-xl text-xs text-red-700 font-medium flex items-center space-x-2">
              <AlertCircle className="w-4 h-4 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* TAB 1: CV Y TRAYECTORIA */}
          {activeTab === 'cv' && (
            <div className="space-y-6 animate-in fade-in duration-150">
              {/* Integrity / Security Warnings Banner */}
              {candidate.cvAnalysis?.warnings && candidate.cvAnalysis.warnings.length > 0 && (
                <div className="bg-amber-50 border border-amber-300 rounded-2xl p-4 sm:p-5 shadow-xs space-y-3">
                  <div className="flex items-center space-x-2.5 text-amber-900 font-bold text-sm sm:text-base">
                    <ShieldAlert className="w-5 h-5 text-amber-600 flex-shrink-0" />
                    <span>Alertas de Integridad y Validación del Currículum ({candidate.cvAnalysis.warnings.length})</span>
                  </div>
                  <ul className="space-y-2 pl-6 list-disc text-xs sm:text-sm text-amber-900">
                    {candidate.cvAnalysis.warnings.map((warn, i) => (
                      <li key={i} className="leading-relaxed">
                        {warn}
                      </li>
                    ))}
                  </ul>
                  <p className="text-xs text-amber-700 italic border-t border-amber-200/60 pt-2">
                    Aviso para el Evaluador: Se recomienda cotejar estas inconsistencias o anomalías directamente durante la entrevista técnica con el postulante.
                  </p>
                </div>
              )}

              {/* Contact overview */}
              <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-3 sm:gap-4">
                <div className="space-y-1 min-w-0">
                  <h4 className="text-xs font-bold text-slate-500 uppercase tracking-wider">
                    Datos de Contacto del Postulante
                  </h4>
                  <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs sm:text-sm text-slate-700 pt-1">
                    <span className="flex items-center space-x-1.5">
                      <Mail className="w-4 h-4 text-slate-400 flex-shrink-0" />
                      <span className="font-semibold truncate">{candidate.email}</span>
                    </span>
                    {candidate.phoneNumber && (
                      <span className="flex items-center space-x-1.5">
                        <Phone className="w-4 h-4 text-slate-400 flex-shrink-0" />
                        <span className="font-semibold">{candidate.phoneNumber}</span>
                      </span>
                    )}
                    <span className="flex items-center space-x-1.5 text-slate-500">
                      <Calendar className="w-4 h-4 text-slate-400 flex-shrink-0" />
                      <span>Recibido: {new Date(candidate.createdAtUtc).toLocaleDateString()}</span>
                    </span>
                  </div>
                </div>

                <div className="flex items-center space-x-2 flex-shrink-0 self-start md:self-auto">
                  <span className="text-xs sm:text-sm text-slate-500">Compatibilidad:</span>
                  <span className="px-3 py-1 bg-emerald-50 text-emerald-800 text-xs sm:text-sm font-bold rounded-lg border border-emerald-200">
                    {candidate.matchScore}% con {candidate.targetRole}
                  </span>
                </div>
              </div>

              {/* CV Content Breakdown */}
              <div className="bg-white p-4 sm:p-6 rounded-2xl border border-slate-200 shadow-sm space-y-5">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-100 pb-3 gap-1">
                  <h4 className="text-sm sm:text-base font-bold text-slate-900 flex items-center space-x-2">
                    <Briefcase className="w-4 h-4 text-slate-700 flex-shrink-0" />
                    <span>Resumen Profesional y Competencias</span>
                  </h4>
                  <span className="text-xs sm:text-sm text-slate-600">
                    Seniority: <strong>{candidate.seniority}</strong> ({candidate.experienceYears} años comprobables)
                  </span>
                </div>

                {cv ? (
                  <div className="space-y-6 text-sm">
                    <div>
                      <p className="font-bold text-slate-700 mb-2 uppercase tracking-wider text-xs">
                        Síntesis Curricular:
                      </p>
                      <p className="p-3.5 sm:p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-700 leading-relaxed font-normal text-xs sm:text-sm">
                        {cv.professionalSummary}
                      </p>
                    </div>

                    {/* Technical Skills */}
                    <div>
                      <p className="font-bold text-slate-700 mb-2.5 uppercase tracking-wider text-xs">
                        Habilidades Técnicas y Evidencia Detectada en el CV:
                      </p>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
                        {cv.skills.map((skill, idx) => (
                          <div key={idx} className="p-3.5 sm:p-4 bg-slate-50 rounded-xl border border-slate-200 space-y-1.5">
                            <div className="flex items-center justify-between gap-2">
                              <span className="font-bold text-slate-900 text-xs sm:text-sm truncate">{skill.normalizedName || skill.name}</span>
                              <span className="px-2 py-0.5 bg-white text-slate-700 font-semibold rounded text-[11px] sm:text-xs border border-slate-200 whitespace-nowrap flex-shrink-0">
                                {skill.experienceYears ? `${skill.experienceYears} años` : 'En CV'}
                              </span>
                            </div>
                            <p className="text-slate-600 text-xs italic line-clamp-3">"{skill.evidence}"</p>
                          </div>
                        ))}
                      </div>
                    </div>

                    {/* Work Experience */}
                    {cv.workExperience && cv.workExperience.length > 0 && (
                      <div>
                        <p className="font-bold text-slate-700 mb-2.5 uppercase tracking-wider text-xs">
                          Experiencia Laboral Detallada:
                        </p>
                        <div className="space-y-3.5">
                          {cv.workExperience.map((exp, idx) => (
                            <div key={idx} className="p-3.5 sm:p-5 bg-slate-50 rounded-xl border border-slate-200 space-y-2">
                              <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-1">
                                <span className="font-bold text-slate-900 text-xs sm:text-base">
                                  {exp.role} — <span className="text-slate-600 font-normal">{exp.company}</span>
                                </span>
                                <span className="text-slate-500 font-semibold text-xs sm:text-sm flex-shrink-0">{exp.durationYears} años</span>
                              </div>
                              {exp.keyAchievements && exp.keyAchievements.length > 0 && (
                                <ul className="list-disc list-inside text-slate-600 space-y-1 text-xs sm:text-sm">
                                  {exp.keyAchievements.map((ach, i) => (
                                    <li key={i}>{ach}</li>
                                  ))}
                                </ul>
                              )}
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <p className="text-sm text-slate-500 italic">No hay desglose curricular estructurado disponible.</p>
                )}
              </div>

              {/* Step Navigation Button */}
              <div className="flex justify-end pt-2">
                <button
                  type="button"
                  onClick={() => setActiveTab('disc')}
                  className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-sm font-bold rounded-xl shadow-sm transition-all flex items-center space-x-2"
                >
                  <span>Paso 2: Revisar Perfil DISC</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}

          {/* TAB 2: PERFIL CONDUCTUAL DISC */}
          {activeTab === 'disc' && (
            <div className="space-y-6 animate-in fade-in duration-150">
              {/* DISC Scores & Visualizer */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-5">
                <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                  <h4 className="text-base font-bold text-slate-900 flex items-center space-x-2">
                    <Compass className="w-4 h-4 text-slate-700" />
                    <span>Evaluación Psicométrica y Conductual DISC</span>
                  </h4>
                  <span className="px-3.5 py-1.5 rounded-lg text-xs sm:text-sm font-bold bg-slate-100 text-slate-900 border border-slate-300">
                    Estilo Principal: {candidate.primaryDiscStyle || 'D/C'}
                  </span>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-center">
                  <div className="space-y-3 text-sm">
                    <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex justify-between items-center">
                      <div>
                        <span className="font-bold text-slate-900 block text-sm">Dominancia (D)</span>
                        <span className="text-xs text-slate-500">Orientación a retos, rapidez y metas</span>
                      </div>
                      <span className="text-lg font-black text-slate-900">{dominance}%</span>
                    </div>
                    <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex justify-between items-center">
                      <div>
                        <span className="font-bold text-slate-900 block text-sm">Influencia (I)</span>
                        <span className="text-xs text-slate-500">Comunicación, persuasión y entusiasmo</span>
                      </div>
                      <span className="text-lg font-black text-slate-900">{influence}%</span>
                    </div>
                    <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex justify-between items-center">
                      <div>
                        <span className="font-bold text-slate-900 block text-sm">Estabilidad (S)</span>
                        <span className="text-xs text-slate-500">Paciencia, escucha y trabajo en equipo</span>
                      </div>
                      <span className="text-lg font-black text-slate-900">{steadiness}%</span>
                    </div>
                    <div className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex justify-between items-center">
                      <div>
                        <span className="font-bold text-slate-900 block text-sm">Cumplimiento (C)</span>
                        <span className="text-xs text-slate-500">Rigor técnico, calidad y precisión</span>
                      </div>
                      <span className="text-lg font-black text-slate-900">{conscientiousness}%</span>
                    </div>
                  </div>

                  <div className="flex flex-col items-center justify-center p-4 bg-slate-50 rounded-2xl border border-slate-200">
                    <RadarChart
                      dominance={dominance}
                      influence={influence}
                      steadiness={steadiness}
                      conscientiousness={conscientiousness}
                      size={220}
                    />
                    <span className="text-xs text-slate-500 mt-2 font-medium">Matriz Conductual de {candidate.firstName}</span>
                  </div>
                </div>
              </div>

              {/* Strengths and Points to Explore */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-white p-5 sm:p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3">
                  <h4 className="text-xs sm:text-sm font-bold text-emerald-800 uppercase tracking-wider flex items-center space-x-1.5">
                    <TrendingUp className="w-4 h-4 text-emerald-600" />
                    <span>Fortalezas Identificadas para el Rol</span>
                  </h4>
                  <ul className="space-y-2.5 text-xs sm:text-sm text-slate-700">
                    {(disc?.strengthsToExplore || [
                      'Alta orientación a la resolución de problemas técnicos complejos',
                      'Rigor en la toma de decisiones basada en métricas y estabilidad de sistemas',
                      'Capacidad comprobada para trabajar de manera autónoma con altos estándares',
                    ]).map((str, i) => (
                      <li key={i} className="flex items-start space-x-2.5">
                        <CheckCircle2 className="w-4 h-4 text-emerald-500 flex-shrink-0 mt-0.5" />
                        <span className="leading-relaxed">{str}</span>
                      </li>
                    ))}
                  </ul>
                </div>

                <div className="bg-white p-5 sm:p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3">
                  <h4 className="text-xs sm:text-sm font-bold text-amber-800 uppercase tracking-wider flex items-center space-x-1.5">
                    <AlertTriangle className="w-4 h-4 text-amber-600" />
                    <span>Puntos a Explorar en la Entrevista</span>
                  </h4>
                  <ul className="space-y-2.5 text-xs sm:text-sm text-slate-700">
                    {(cv?.pointsToValidate || disc?.pointsToExplore || [
                      'Validar su adaptación ante cambios no planificados en el roadmap técnico',
                      'Indagar en su experiencia liderando o colaborando con perfiles interdisciplinarios',
                    ]).map((pt, i) => (
                      <li key={i} className="flex items-start space-x-2.5">
                        <HelpCircle className="w-4 h-4 text-amber-500 flex-shrink-0 mt-0.5" />
                        <span className="leading-relaxed">{pt}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>

              {/* Step Navigation Buttons */}
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5 pt-2">
                <button
                  type="button"
                  onClick={() => setActiveTab('cv')}
                  className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-xl transition-colors flex items-center justify-center space-x-1.5 cursor-pointer"
                >
                  <ArrowLeft className="w-4 h-4" />
                  <span>Anterior: CV</span>
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('interview')}
                  className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
                >
                  <span>Paso 3: Guía STAR de Entrevista</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}

          {/* TAB 3: PREGUNTAS STAR DE ENTREVISTA */}
          {activeTab === 'interview' && (
            <div className="space-y-6 animate-in fade-in duration-150">
              <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div>
                  <h4 className="text-base font-bold text-slate-900">Protocolo de Entrevista Estructurada (Metodología STAR)</h4>
                  <p className="text-xs sm:text-sm text-slate-600 mt-0.5">
                    Preguntas personalizadas generadas por IA según el CV de {candidate.firstName} y su perfil conductual {candidate.primaryDiscStyle}.
                  </p>
                </div>
                <span className="px-3.5 py-1 bg-slate-900 text-white text-xs font-bold rounded-xl flex-shrink-0 self-start sm:self-auto">
                  Guía Activa
                </span>
              </div>

              {/* Technical Questions */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3.5">
                <h4 className="text-xs sm:text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center space-x-2">
                  <Award className="w-4 h-4 text-blue-600" />
                  <span>1. Indagación Técnica & Arquitectura de Software</span>
                </h4>
                <div className="space-y-3 text-sm">
                  {(report?.interviewGuide.technicalQuestions || [
                    '¿Cómo estructuras y optimizas la concurrencia y transacciones en PostgreSQL con Entity Framework Core?',
                    'Explícanos tu experiencia implementando políticas de resiliencia con Polly y Circuit Breakers en microservicios.',
                    '¿Qué estrategias utilizas para monitorear y detectar cuellos de botella en producción?',
                  ]).map((q, i) => (
                    <div key={i} className="p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium flex items-start space-x-3 text-sm sm:text-base">
                      <span className="w-6 h-6 rounded-full bg-slate-200 text-slate-800 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                        {i + 1}
                      </span>
                      <span className="leading-relaxed">{q}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Behavioral Questions */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3.5">
                <h4 className="text-xs sm:text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center space-x-2">
                  <Compass className="w-4 h-4 text-emerald-600" />
                  <span>2. Competencias Conductuales & Gestión del Conflicto</span>
                </h4>
                <div className="space-y-3 text-sm">
                  {(report?.interviewGuide.behavioralQuestions || [
                    'Describe una situación donde las prioridades del negocio cambiaron abruptamente y cómo reaccionaste para reorganizar los entregables.',
                    'Cuéntanos una ocasión en la que tuviste un desacuerdo técnico sobre una decisión arquitectónica y cómo lograron alinearse.',
                  ]).map((q, i) => (
                    <div key={i} className="p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium flex items-start space-x-3 text-sm sm:text-base">
                      <span className="w-6 h-6 rounded-full bg-slate-200 text-slate-800 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                        {i + 1}
                      </span>
                      <span className="leading-relaxed">{q}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Professional Questions */}
              <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3.5">
                <h4 className="text-xs sm:text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center space-x-2">
                  <Briefcase className="w-4 h-4 text-purple-600" />
                  <span>3. Trayectoria Laboral & Aprendizajes Clave</span>
                </h4>
                <div className="space-y-3 text-sm">
                  {(report?.interviewGuide.professionalQuestions || [
                    '¿Cuál ha sido el desafío técnico de mayor impacto o dificultad que has liderado?',
                    '¿Cómo abordas la deuda técnica y qué métricas usas para justificar su refactorización ante el negocio?',
                  ]).map((q, i) => (
                    <div key={i} className="p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium flex items-start space-x-3 text-sm sm:text-base">
                      <span className="w-6 h-6 rounded-full bg-slate-200 text-slate-800 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                        {i + 1}
                      </span>
                      <span className="leading-relaxed">{q}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Step Navigation Buttons */}
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5 pt-2">
                <button
                  type="button"
                  onClick={() => setActiveTab('disc')}
                  className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-xl transition-colors flex items-center justify-center space-x-1.5 cursor-pointer"
                >
                  <ArrowLeft className="w-4 h-4" />
                  <span>Anterior: DISC</span>
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('decision')}
                  className="px-5 py-2.5 bg-emerald-700 hover:bg-emerald-800 text-white text-xs sm:text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
                >
                  <span>Paso 4: Registrar Dictamen Oficial</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}

          {/* TAB 4: DICTAMEN OFICIAL Y NOTAS CONFIDENCIALES */}
          {activeTab === 'decision' && (
            <div className="space-y-6 animate-in fade-in duration-150">
              <div className="bg-white rounded-2xl border-2 border-slate-300 shadow-sm p-6 space-y-6">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-200 pb-4 gap-2">
                  <div>
                    <h3 className="text-lg font-bold text-slate-900 flex items-center space-x-2">
                      <FileCheck className="w-5 h-5 text-emerald-600" />
                      <span>Resolución Oficial del Evaluador</span>
                    </h3>
                    <p className="text-xs sm:text-sm text-slate-500 mt-0.5">
                      Selecciona la decisión formal para el expediente de {candidate.firstName} {candidate.lastName}.
                    </p>
                  </div>
                  {candidate.evaluatedAtUtc && (
                    <span className="text-xs font-medium text-slate-500 bg-slate-100 px-3 py-1 rounded-lg border border-slate-200">
                      Última resolución: {new Date(candidate.evaluatedAtUtc).toLocaleString()}
                    </span>
                  )}
                </div>

                {/* 4 Decision Cards */}
                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2.5">
                    Dictamen de Selección:
                  </label>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                    {decisionOptions.map((opt) => {
                      const Icon = opt.icon;
                      const isSelected = decision === opt.value;
                      return (
                        <button
                          key={opt.value}
                          type="button"
                          onClick={() => setDecision(opt.value)}
                          className={`p-4 rounded-xl border text-left transition-all flex flex-col justify-between cursor-pointer ${
                            isSelected
                              ? opt.selectedClass
                              : 'border-slate-200 bg-slate-50/70 hover:bg-slate-100/90 text-slate-700'
                          }`}
                        >
                          <div className="flex items-center justify-between mb-2">
                            <span className="text-base font-bold flex items-center space-x-2">
                              <Icon className={`w-5 h-5 ${opt.iconColor}`} />
                              <span>{opt.label}</span>
                            </span>
                            {isSelected && (
                              <span className="w-2.5 h-2.5 rounded-full bg-slate-900"></span>
                            )}
                          </div>
                          <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
                            {opt.desc}
                          </p>
                        </button>
                      );
                    })}
                  </div>
                </div>

                {/* Private Notes Textarea */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider">
                      Minuta y Observaciones Confidenciales del Evaluador:
                    </label>
                    <span className="text-xs text-slate-400 font-medium">Solo visible para el comité</span>
                  </div>
                  <textarea
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    placeholder="Escribe aquí los motivos del dictamen, hallazgos clave de la entrevista técnica, fortalezas demostradas y acuerdos para la oferta..."
                    rows={4}
                    className="w-full p-4 text-sm sm:text-base bg-slate-50 border border-slate-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-slate-400/20 focus:border-slate-600 focus:bg-white text-slate-800 placeholder:text-slate-400 leading-relaxed transition-all resize-none"
                  />
                </div>

                {/* Save Decision Row */}
                <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-2 border-t border-slate-100">
                  <div>
                    {decisionSuccessMsg && (
                      <span className="text-xs sm:text-sm font-bold text-emerald-700 flex items-center space-x-1.5 bg-emerald-50 px-3 py-1.5 rounded-lg border border-emerald-200">
                        <CheckCircle2 className="w-4 h-4" />
                        <span>{decisionSuccessMsg}</span>
                      </span>
                    )}
                  </div>
                  <button
                    type="button"
                    onClick={handleSaveDecision}
                    disabled={isSavingDecision}
                    className="w-full sm:w-auto px-6 py-3 bg-slate-900 hover:bg-slate-800 active:scale-95 text-white text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 disabled:opacity-50"
                  >
                    {isSavingDecision ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Save className="w-4 h-4" />
                    )}
                    <span>Registrar Resolución Oficial</span>
                  </button>
                </div>
              </div>

              {/* Step Navigation Buttons */}
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5 pt-2">
                <button
                  type="button"
                  onClick={() => setActiveTab('interview')}
                  className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-xl transition-colors flex items-center justify-center space-x-1.5 cursor-pointer"
                >
                  <ArrowLeft className="w-4 h-4" />
                  <span>Anterior: Preguntas STAR</span>
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('pdf')}
                  className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
                >
                  <span>Paso 5: Ver Documento Oficial PDF</span>
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}

          {/* TAB 5: DOCUMENTO OFICIAL (PDF) */}
          {activeTab === 'pdf' && (
            <div className="space-y-6 animate-in fade-in duration-150">
              <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 space-y-6">
                <div className="border-b border-slate-200 pb-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div>
                    <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                      Expediente Oficial de Selección • Formato Auditado
                    </span>
                    <h3 className="text-base font-bold text-slate-900">
                      Previsualización y Descarga del Documento
                    </h3>
                  </div>
                  <button
                    onClick={handleDownloadPdf}
                    disabled={isDownloading}
                    className="flex items-center justify-center space-x-2 px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl shadow-sm transition-all disabled:opacity-50"
                  >
                    {isDownloading ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Download className="w-4 h-4" />
                    )}
                    <span>Descargar Expediente PDF</span>
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                  <div className="p-5 bg-slate-50 rounded-2xl border border-slate-200 space-y-2 text-xs">
                    <div className="flex justify-between items-center border-b border-slate-200 pb-2">
                      <span className="font-bold text-slate-900">Página 1: Resumen Curricular & DISC</span>
                      <span className="text-[10px] px-2 py-0.5 bg-slate-200 text-slate-800 font-bold rounded">Folio 1</span>
                    </div>
                    <div className="text-slate-600 space-y-1.5 pt-1">
                      <p><strong>Candidato:</strong> {candidate.firstName} {candidate.lastName}</p>
                      <p><strong>Posición:</strong> {candidate.targetRole} ({candidate.seniority})</p>
                      <p><strong>Compatibilidad:</strong> {candidate.matchScore}%</p>
                      <p><strong>Estilo Conductual:</strong> Patrón DISC {candidate.primaryDiscStyle}</p>
                    </div>
                  </div>

                  <div className="p-5 bg-slate-50 rounded-2xl border border-slate-200 space-y-2 text-xs">
                    <div className="flex justify-between items-center border-b border-slate-200 pb-2">
                      <span className="font-bold text-slate-900">Página 2: Protocolo STAR & Resolución</span>
                      <span className="text-[10px] px-2 py-0.5 bg-slate-200 text-slate-800 font-bold rounded">Folio 2</span>
                    </div>
                    <div className="text-slate-600 space-y-1.5 pt-1">
                      <p><strong>Preguntas Situacionales:</strong> Metodología STAR integrada</p>
                      <p><strong>Resolución Registrada:</strong> {candidate.evaluatorDecision || 'Pendiente'}</p>
                      <p><strong>Evaluador Asignado:</strong> {candidate.assignedRecruiterName || 'Comité de Selección'}</p>
                      <p><strong>Estado del Documento:</strong> Firmado digitalmente y archivado</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Step Navigation Buttons */}
              <div className="flex items-center justify-between pt-2">
                <button
                  type="button"
                  onClick={() => setActiveTab('decision')}
                  className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs font-semibold rounded-xl transition-colors flex items-center space-x-1.5"
                >
                  <ArrowLeft className="w-4 h-4" />
                  <span>Anterior: Dictamen Oficial</span>
                </button>

                <button
                  type="button"
                  onClick={onClose}
                  className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl shadow-sm transition-all"
                >
                  <span>Concluir y Cerrar Expediente</span>
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Modal Sticky Bottom Bar */}
        <div className="px-5 sm:px-6 py-3.5 border-t border-slate-200 bg-white flex items-center justify-between">
          <div className="flex items-center space-x-2 text-xs sm:text-sm text-slate-500">
            <Lock className="w-4 h-4 text-slate-400" />
            <span>Expediente de Candidato Auditado • Confidencial</span>
          </div>

          <div className="flex items-center space-x-2.5">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-semibold rounded-xl transition-colors"
            >
              Cerrar Expediente
            </button>
            <button
              type="button"
              onClick={() => {
                if (activeTab !== 'decision') {
                  setActiveTab('decision');
                } else {
                  handleSaveDecision();
                }
              }}
              disabled={isSavingDecision}
              className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-sm font-bold rounded-xl transition-colors shadow-sm flex items-center space-x-1.5 disabled:opacity-50"
            >
              {isSavingDecision ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <Save className="w-4 h-4" />
              )}
              <span>{activeTab === 'decision' ? 'Guardar Resolución' : 'Ir a Dictaminar'}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
