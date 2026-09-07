import { useState, useEffect, useCallback } from 'react';
import { Navbar } from './components/Navbar';
import { StatsCards } from './components/StatsCards';
import { EvaluatorCandidateDashboard } from './components/EvaluatorCandidateDashboard';
import { EvaluatorReviewModal } from './components/EvaluatorReviewModal';
import { AssignRecruiterModal } from './components/AssignRecruiterModal';
import { UploadCandidateModal } from './components/UploadCandidateModal';
import { CreateJobPositionModal } from './components/CreateJobPositionModal';
import { QuickStartGuide } from './components/QuickStartGuide';
import { AuthProvider, useAuth } from './context/AuthContext';
import { Candidate, JobPosition } from './types';
import { api } from './services/api';
import { ShieldCheck, CheckCircle2, AlertCircle, UserCheck } from 'lucide-react';

function AppContent() {
  const { user, isAdmin, isAuthenticated, login } = useAuth();
  const [candidates, setCandidates] = useState<Candidate[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isBackendHealthy, setIsBackendHealthy] = useState<boolean | null>(null);

  // Active Evaluator Candidate for Review
  const [selectedCandidate, setSelectedCandidate] = useState<Candidate | null>(null);

  // Candidate for Recruiter Assignment Modal
  const [assigningCandidate, setAssigningCandidate] = useState<Candidate | null>(null);

  // Upload Candidate Modal & Create Position Modal
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [isPositionModalOpen, setIsPositionModalOpen] = useState(false);

  // Notification Toast
  const [notification, setNotification] = useState<{
    type: 'success' | 'error' | 'info';
    message: string;
  } | null>(null);

  const checkHealth = useCallback(async () => {
    try {
      const health = await api.getHealth();
      setIsBackendHealthy(health.status === 'Healthy');
    } catch {
      setIsBackendHealthy(false);
    }
  }, []);

  const fetchCandidates = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await api.getCandidates();
      setCandidates(data);
    } catch (err: any) {
      console.error('Error fetching candidates:', err);
      setNotification({ type: 'error', message: 'Error al conectar con la API de candidatos.' });
      setTimeout(() => setNotification(null), 4000);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleCandidateUpdated = (updated: Candidate) => {
    setCandidates((prev) =>
      prev.map((c) => (c.id === updated.id ? updated : c))
    );
    if (selectedCandidate?.id === updated.id) {
      setSelectedCandidate(updated);
    }
    setNotification({
      type: 'success',
      message: updated.assignedRecruiterName
        ? `Expediente de ${updated.firstName} ${updated.lastName} asignado a ${updated.assignedRecruiterName}.`
        : `Expediente de ${updated.firstName} ${updated.lastName} actualizado exitosamente.`,
    });
    setTimeout(() => setNotification(null), 3500);
  };

  const handleCandidateIngested = (newCandidate: Candidate) => {
    setCandidates((prev) => [newCandidate, ...prev.filter((c) => c.id !== newCandidate.id)]);
    setSelectedCandidate(newCandidate);
    setNotification({
      type: 'success',
      message: `Postulante ${newCandidate.firstName} ${newCandidate.lastName} evaluado exitosamente con Gemini AI.`,
    });
    setTimeout(() => setNotification(null), 4500);
  };

  const handlePositionCreated = (newPosition: JobPosition) => {
    setNotification({
      type: 'success',
      message: `Nueva vacante "${newPosition.title}" registrada exitosamente.`,
    });
    setTimeout(() => setNotification(null), 4000);
    fetchCandidates();
  };

  useEffect(() => {
    if (isAuthenticated) {
      checkHealth();
      fetchCandidates();
    }
  }, [isAuthenticated, checkHealth, fetchCandidates]);

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-slate-900 flex flex-col items-center justify-center p-4">
        <div className="max-w-md w-full bg-slate-800 border border-slate-700 rounded-2xl p-8 text-center space-y-5 shadow-2xl">
          <div className="w-14 h-14 rounded-2xl bg-amber-500/10 border border-amber-500/30 text-amber-400 flex items-center justify-center mx-auto">
            <ShieldCheck className="w-7 h-7" />
          </div>
          <div className="space-y-2">
            <h2 className="text-base font-bold text-white">Autenticación Requerida</h2>
            <p className="text-xs text-slate-400 leading-relaxed">
              Tu sesión no está activa o ha expirado. Para ingresar a TalentIQ ATS debes identificarte en el servidor de identidad.
            </p>
          </div>
          <button
            onClick={login}
            className="w-full py-3 bg-slate-100 hover:bg-white text-slate-900 font-bold text-xs rounded-xl transition-all shadow-md flex items-center justify-center space-x-2 cursor-pointer"
          >
            <UserCheck className="w-4 h-4" />
            <span>Iniciar Sesión en Keycloak</span>
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col selection:bg-brand-500 selection:text-white">
      {/* Top Navigation */}
      <Navbar
        isBackendHealthy={isBackendHealthy}
        onRefresh={fetchCandidates}
        isLoading={isLoading}
        onOpenUploadModal={() => setIsUploadModalOpen(true)}
        onOpenPositionModal={() => setIsPositionModalOpen(true)}
      />

      {/* Global Notification Toast */}
      {notification && (
        <div
          className={`fixed bottom-4 sm:bottom-5 left-4 right-4 sm:left-auto sm:right-5 sm:max-w-md z-50 p-3.5 sm:p-4 rounded-xl shadow-xl border flex items-center space-x-3 transition-all animate-slide-up ${
            notification.type === 'success'
              ? 'bg-slate-900 text-emerald-400 border-emerald-500/30'
              : notification.type === 'error'
              ? 'bg-slate-900 text-red-400 border-red-500/30'
              : 'bg-slate-900 text-slate-100 border-slate-700'
          }`}
        >
          {notification.type === 'success' && <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />}
          {notification.type === 'error' && <AlertCircle className="w-5 h-5 text-red-400 shrink-0" />}
          <span className="text-xs sm:text-sm font-semibold text-white leading-snug">{notification.message}</span>
        </div>
      )}

      {/* Main Content */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-6 space-y-4 sm:space-y-5">
        {/* Page Header - Clean & Non-Fatiguing */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-1">
          <div>
            <div className="flex items-center space-x-2 text-xs sm:text-sm text-slate-500 font-medium mb-1">
              {isAdmin ? (
                <span className="inline-flex items-center space-x-1.5 text-slate-600">
                  <ShieldCheck className="w-4 h-4 text-purple-600 shrink-0" />
                  <span>Panel de Administración</span>
                </span>
              ) : (
                <span className="inline-flex items-center space-x-1.5 text-slate-600">
                  <UserCheck className="w-4 h-4 text-emerald-600 shrink-0" />
                  <span>Panel de Reclutador</span>
                </span>
              )}
              <span>·</span>
              <span>TalentIQ Enterprise ATS</span>
            </div>
            <h1 className="text-xl sm:text-2xl md:text-3xl font-bold text-slate-900 tracking-tight">
              {isAdmin ? 'Gestión de Postulantes & Delegación' : `Mis Expedientes Asignados`}
            </h1>
            <p className="text-xs sm:text-sm text-slate-500 mt-1">
              {isAdmin
                ? 'Supervisión de candidatos, asignación a evaluadores y seguimiento de dictámenes.'
                : 'Expedientes asignados para revisión de perfil, aplicación de preguntas STAR y dictamen oficial.'}
            </p>
          </div>

          <div className="flex items-center space-x-2 text-xs sm:text-sm text-slate-600 bg-white px-3.5 py-2 rounded-lg border border-slate-200/80 shadow-2xs self-start sm:self-auto shrink-0">
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
            <span className="font-semibold text-slate-800">{user?.fullName}</span>
            <span className="text-slate-300">·</span>
            <span className="text-slate-500">{isAdmin ? 'Admin' : 'Evaluador'}</span>
          </div>
        </div>

        {/* Intuitive Quick-Start Guide */}
        <QuickStartGuide />

        {/* High-level Evaluator Metrics */}
        <StatsCards candidates={candidates} />

        {/* Evaluator Grouped Candidates Dashboard */}
        <EvaluatorCandidateDashboard
          candidates={candidates}
          onSelectCandidate={(c) => setSelectedCandidate(c)}
          onAssignCandidate={(c) => setAssigningCandidate(c)}
        />
      </main>

      {/* Read-Only Evaluator Dossier Modal */}
      <EvaluatorReviewModal
        isOpen={!!selectedCandidate}
        candidate={selectedCandidate}
        onClose={() => setSelectedCandidate(null)}
        onCandidateUpdated={handleCandidateUpdated}
      />

      {/* Assign Recruiter Modal (Admin Only) */}
      <AssignRecruiterModal
        isOpen={!!assigningCandidate}
        candidate={assigningCandidate}
        onClose={() => setAssigningCandidate(null)}
        onAssigned={handleCandidateUpdated}
      />

      {/* Direct CV Ingestion & Real-Time Gemini AI Evaluation Modal */}
      <UploadCandidateModal
        isOpen={isUploadModalOpen}
        onClose={() => setIsUploadModalOpen(false)}
        onCandidateIngested={handleCandidateIngested}
      />

      {/* Create New Job Vacancy Modal (Admin Only) */}
      <CreateJobPositionModal
        isOpen={isPositionModalOpen}
        onClose={() => setIsPositionModalOpen(false)}
        onPositionCreated={handlePositionCreated}
      />

      {/* Footer */}
      <footer className="mt-auto border-t border-slate-200 bg-white py-5 sm:py-6">
        <div className="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-2.5 sm:gap-3 text-xs text-slate-500 text-center sm:text-left">
          <div>
            TalentIQ Enterprise ATS • Módulo de Selección y Evaluación de Talento
          </div>
          <div className="flex flex-wrap items-center justify-center sm:justify-end gap-x-2.5 gap-y-1 text-slate-400 text-[11px]">
            <span>Entorno Seguro (Keycloak IAM)</span>
            <span>•</span>
            <span>Documentación Confidencial</span>
            <span>•</span>
            <span>v2.5 Enterprise RBAC</span>
          </div>
        </div>
      </footer>
    </div>
  );
}

export function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

export default App;
