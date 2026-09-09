import React, { useState, useEffect } from 'react';
import {
  X,
  Download,
  FileText,
  Compass,
  HelpCircle,
  AlertCircle,
  Briefcase,
  CheckCircle2,
  Loader2,
  Lock,
  Save,
  Clock,
  XCircle,
  Building2,
  FileCheck
} from 'lucide-react';
import { Candidate } from '../types';
import { api } from '../services/api';
import { CvAnalysisTab } from './evaluator/CvAnalysisTab';
import { DiscProfileTab } from './evaluator/DiscProfileTab';
import { InterviewGuideTab } from './evaluator/InterviewGuideTab';
import { DecisionTab } from './evaluator/DecisionTab';
import { PdfReportTab } from './evaluator/PdfReportTab';
import { PdfViewerModal } from './PdfViewerModal';

interface EvaluatorReviewModalProps {
  isOpen: boolean;
  candidate: Candidate | null;
  onClose: () => void;
  onCandidateUpdated?: (updated: Candidate) => void;
}

type ModalTab = 'cv' | 'disc' | 'interview' | 'decision' | 'pdf';

export const EvaluatorReviewModal: React.FC<EvaluatorReviewModalProps> = ({
  isOpen,
  candidate,
  onClose,
  onCandidateUpdated,
}) => {
  const [activeTab, setActiveTab] = useState<ModalTab>('cv');
  const [isDownloading, setIsDownloading] = useState(false);
  const [isPdfModalOpen, setIsPdfModalOpen] = useState(false);
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
                <span
                  className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-bold bg-slate-800 text-emerald-400 border border-slate-700"
                  title="Cotejo informativo de tecnologías y experiencia detectadas en el CV frente a los requisitos de la vacante. No representa una calificación."
                >
                  Cotejo: {candidate.matchScore ?? 0}% requisitos
                </span>
                <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-semibold bg-blue-900/60 text-blue-300 border border-blue-700/60">
                  Dossier Pre-Entrevista
                </span>
                <span className="px-2 sm:px-2.5 py-0.5 rounded-full text-[11px] sm:text-xs font-semibold bg-slate-800 text-slate-300 border border-slate-700">
                  DISC: {candidate.primaryDiscStyle || 'Sin evaluar'}
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
              type="button"
              onClick={() => setIsPdfModalOpen(true)}
              className="flex items-center space-x-1.5 px-2.5 py-1.5 sm:px-3.5 sm:py-2 bg-blue-600 hover:bg-blue-500 text-white text-xs sm:text-sm font-semibold rounded-lg shadow-2xs transition-all cursor-pointer"
              title="Abrir visor interactivo del currículum original en la web"
            >
              <FileText className="w-4 h-4" />
              <span className="hidden xs:inline">Ver CV</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('pdf')}
              className={`flex items-center space-x-1.5 px-2.5 py-1.5 sm:px-3 sm:py-2 rounded-lg text-xs sm:text-sm font-semibold transition-all cursor-pointer ${
                activeTab === 'pdf'
                  ? 'bg-emerald-600 text-white'
                  : 'bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700'
              }`}
              title="Ver informe pre-entrevista generado en el visor web"
            >
              <FileCheck className="w-4 h-4" />
              <span className="hidden xs:inline">Ver Expediente</span>
            </button>

            <button
              onClick={handleDownloadPdf}
              disabled={isDownloading}
              className="hidden md:flex items-center space-x-1.5 px-3 py-1.5 sm:px-3 sm:py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 hover:text-white border border-slate-700 text-xs sm:text-sm font-semibold rounded-lg transition-all disabled:opacity-50 cursor-pointer"
              title="Descargar archivo PDF oficial del expediente"
            >
              {isDownloading ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <Download className="w-4 h-4" />
              )}
              <span>Descargar</span>
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
              className={`flex-1 min-w-[140px] sm:min-w-0 flex items-center justify-center space-x-2 py-2.5 px-3 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'cv'
                  ? 'bg-white text-slate-900 shadow-sm font-bold'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
              }`}
            >
              <Briefcase className={`w-4 h-4 flex-shrink-0 ${activeTab === 'cv' ? 'text-slate-900' : 'text-slate-500'}`} />
              <span className="truncate">1. Síntesis & CV Original</span>
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
              <span className="truncate">2. Perfil Psicométrico</span>
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
              <span className="truncate">4. Dictamen RRHH</span>
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

          {activeTab === 'cv' && (
            <CvAnalysisTab
              candidate={candidate}
              onNext={() => setActiveTab('disc')}
              onOpenPdfModal={() => setIsPdfModalOpen(true)}
            />
          )}

          {activeTab === 'disc' && (
            <DiscProfileTab
              candidate={candidate}
              onPrev={() => setActiveTab('cv')}
              onNext={() => setActiveTab('interview')}
            />
          )}

          {activeTab === 'interview' && (
            <InterviewGuideTab
              candidate={candidate}
              onPrev={() => setActiveTab('disc')}
              onNext={() => setActiveTab('decision')}
            />
          )}

          {activeTab === 'decision' && (
            <DecisionTab
              candidate={candidate}
              decision={decision}
              setDecision={setDecision}
              notes={notes}
              setNotes={setNotes}
              decisionSuccessMsg={decisionSuccessMsg}
              isSavingDecision={isSavingDecision}
              onSaveDecision={handleSaveDecision}
              onPrev={() => setActiveTab('interview')}
              onNext={() => setActiveTab('pdf')}
            />
          )}

          {activeTab === 'pdf' && (
            <PdfReportTab
              candidate={candidate}
              isDownloading={isDownloading}
              onDownloadPdf={handleDownloadPdf}
              onPrev={() => setActiveTab('decision')}
              onClose={onClose}
            />
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
              className="px-4 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-sm font-semibold rounded-xl transition-colors cursor-pointer"
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
              className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-sm font-bold rounded-xl transition-colors shadow-sm flex items-center space-x-1.5 disabled:opacity-50 cursor-pointer"
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

      {/* Visor Web Embebido del CV Original */}
      <PdfViewerModal
        isOpen={isPdfModalOpen}
        candidateId={candidate.id}
        candidateName={`${candidate.firstName} ${candidate.lastName}`}
        onClose={() => setIsPdfModalOpen(false)}
      />
    </div>
  );
};
