import React, { useState, useEffect } from 'react';
import { X, UserCheck, Loader2, CheckCircle2, User } from 'lucide-react';
import { Candidate, Recruiter } from '../types';
import { api } from '../services/api';

interface AssignRecruiterModalProps {
  isOpen: boolean;
  candidate: Candidate | null;
  onClose: () => void;
  onAssigned: (updatedCandidate: Candidate) => void;
}

export const AssignRecruiterModal: React.FC<AssignRecruiterModalProps> = ({
  isOpen,
  candidate,
  onClose,
  onAssigned,
}) => {
  const [recruiters, setRecruiters] = useState<Recruiter[]>([]);
  const [selectedRecruiterId, setSelectedRecruiterId] = useState<string>('');
  const [isLoadingRecruiters, setIsLoadingRecruiters] = useState<boolean>(false);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen && candidate) {
      setSelectedRecruiterId(candidate.assignedRecruiterId || '');
      fetchRecruiters();
    }
  }, [isOpen, candidate]);

  const fetchRecruiters = async () => {
    setIsLoadingRecruiters(true);
    try {
      const data = await api.getRecruiters();
      setRecruiters(data);
    } catch (err) {
      console.error('Error al obtener reclutadores:', err);
      setErrorMsg('No se pudo cargar la lista de evaluadores disponibles.');
    } finally {
      setIsLoadingRecruiters(false);
    }
  };

  const handleAssign = async () => {
    if (!candidate) return;
    setIsSaving(true);
    setErrorMsg(null);

    const targetRecruiter = recruiters.find((r) => r.id === selectedRecruiterId);
    if (!targetRecruiter && selectedRecruiterId !== '') {
      setErrorMsg('Seleccione un evaluador válido.');
      setIsSaving(false);
      return;
    }

    try {
      const updated = await api.assignCandidate(
        candidate.id,
        targetRecruiter ? targetRecruiter.id : '',
        targetRecruiter ? targetRecruiter.fullName : '',
        targetRecruiter ? targetRecruiter.email : ''
      );
      onAssigned(updated);
      onClose();
    } catch (err: any) {
      setErrorMsg('Error al registrar la asignación.');
    } finally {
      setIsSaving(false);
    }
  };

  if (!isOpen || !candidate) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-2 sm:p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 max-w-lg w-full overflow-hidden flex flex-col h-[95vh] sm:h-auto sm:max-h-[90vh] animate-in fade-in zoom-in duration-150">
        {/* Header */}
        <div className="px-4 sm:px-6 py-3.5 sm:py-5 bg-slate-900 text-white flex items-center justify-between gap-3">
          <div className="flex items-center space-x-3 min-w-0">
            <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-300 flex-shrink-0">
              <UserCheck className="w-4 h-4 sm:w-5 sm:h-5" />
            </div>
            <div className="min-w-0">
              <h3 className="text-sm sm:text-base font-bold tracking-wide truncate">Delegación de Expediente</h3>
              <p className="text-[11px] sm:text-xs text-slate-400 truncate">Asignar evaluador para diagnóstico y entrevista</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer flex-shrink-0"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-4 sm:p-6 space-y-4 sm:space-y-5 flex-1 overflow-y-auto">
          {/* Candidate Summary */}
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl space-y-1 text-sm">
            <div className="font-bold text-slate-900 text-base">
              {candidate.firstName} {candidate.lastName}
            </div>
            <div className="text-slate-600">
              <span className="font-semibold text-slate-700">Posición:</span> {candidate.targetRole} • {candidate.seniority}
            </div>
            <div className="text-slate-500 text-xs">
              {candidate.email}
            </div>
          </div>

          {/* Current Assignment Status */}
          <div className="text-sm">
            <span className="text-slate-500">Estado de asignación actual: </span>
            {candidate.assignedRecruiterName ? (
              <span className="font-bold text-slate-900">
                Asignado a {candidate.assignedRecruiterName} ({candidate.assignedRecruiterEmail})
              </span>
            ) : (
              <span className="font-semibold text-amber-700 bg-amber-50 px-2.5 py-0.5 rounded border border-amber-200">
                Sin asignar
              </span>
            )}
          </div>

          {/* Recruiter Selector */}
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2.5">
              Seleccionar Reclutador / Evaluador Asignado:
            </label>

            {isLoadingRecruiters ? (
              <div className="flex items-center space-x-2 text-xs text-slate-500 py-3">
                <Loader2 className="w-4 h-4 animate-spin text-slate-700" />
                <span>Cargando evaluadores disponibles...</span>
              </div>
            ) : (
              <div className="space-y-2 max-h-56 overflow-y-auto pr-1">
                {recruiters.map((r) => {
                  const isSelected = selectedRecruiterId === r.id;
                  return (
                    <button
                      key={r.id}
                      type="button"
                      onClick={() => setSelectedRecruiterId(r.id)}
                      className={`w-full p-3.5 rounded-xl border text-left flex items-center justify-between transition-all ${
                        isSelected
                          ? 'border-slate-900 bg-slate-900 text-white shadow-sm'
                          : 'border-slate-200 bg-white hover:bg-slate-50 text-slate-800'
                      }`}
                    >
                      <div className="flex items-center space-x-3">
                        <div
                          className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-xs ${
                            isSelected ? 'bg-slate-800 text-slate-200' : 'bg-slate-100 text-slate-700'
                          }`}
                        >
                          <User className="w-4 h-4" />
                        </div>
                        <div>
                          <div className={`text-sm font-bold ${isSelected ? 'text-white' : 'text-slate-900'}`}>
                            {r.fullName}
                          </div>
                          <div className={`text-xs ${isSelected ? 'text-slate-300' : 'text-slate-500'}`}>
                            {r.email}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center space-x-2">
                        <span
                          className={`text-xs px-2.5 py-0.5 rounded-full font-semibold ${
                            isSelected
                              ? 'bg-slate-800 text-slate-300 border border-slate-700'
                              : 'bg-slate-100 text-slate-600'
                          }`}
                        >
                          {r.activeAssignmentsCount} asignados
                        </span>
                        {isSelected && <CheckCircle2 className="w-4 h-4 text-emerald-400" />}
                      </div>
                    </button>
                  );
                })}

                {/* Option to unassign */}
                <button
                  type="button"
                  onClick={() => setSelectedRecruiterId('')}
                  className={`w-full p-3 rounded-xl border text-xs sm:text-sm text-center font-medium transition-all ${
                    selectedRecruiterId === ''
                      ? 'border-amber-400 bg-amber-50 text-amber-900 font-bold'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  Dejar sin asignar (remover asignación actual)
                </button>
              </div>
            )}
          </div>

          {errorMsg && (
            <div className="p-3 bg-red-50 text-red-700 text-sm rounded-lg border border-red-200 font-medium">
              {errorMsg}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-4 sm:px-6 py-3.5 sm:py-4 bg-slate-50 border-t border-slate-200 flex items-center justify-end space-x-2.5">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 sm:flex-none px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-lg transition-colors cursor-pointer text-center"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={handleAssign}
            disabled={isSaving}
            className="flex-1 sm:flex-none px-5 py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-semibold rounded-lg shadow-sm transition-all flex items-center justify-center space-x-1.5 disabled:opacity-50 cursor-pointer"
          >
            {isSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserCheck className="w-4 h-4" />}
            <span>Confirmar Asignación</span>
          </button>
        </div>
      </div>
    </div>
  );
};
