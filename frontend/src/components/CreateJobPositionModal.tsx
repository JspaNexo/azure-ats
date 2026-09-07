import React, { useState } from 'react';
import { X, Briefcase, Loader2, CheckCircle2 } from 'lucide-react';
import { JobPosition } from '../types';
import { api } from '../services/api';

interface CreateJobPositionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onPositionCreated: (position: JobPosition) => void;
}

const DEPARTMENTS = [
  'Tecnología & Arquitectura',
  'Infraestructura Cloud & DevOps',
  'Datos & Inteligencia Artificial',
  'Desarrollo de Software',
  'Ciberseguridad',
  'Producto & Diseño UX',
  'Operaciones & Procesos',
];

const SENIORITIES = ['Junior', 'Semi-Senior', 'Senior', 'Lead / Principal'];

export const CreateJobPositionModal: React.FC<CreateJobPositionModalProps> = ({
  isOpen,
  onClose,
  onPositionCreated,
}) => {
  const [title, setTitle] = useState('');
  const [department, setDepartment] = useState(DEPARTMENTS[0]);
  const [seniority, setSeniority] = useState('Senior');
  const [minExperienceYears, setMinExperienceYears] = useState<number>(3);
  const [description, setDescription] = useState('');
  const [requirements, setRequirements] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) {
      setErrorMsg('El título de la vacante es obligatorio.');
      return;
    }
    if (!department.trim()) {
      setErrorMsg('El departamento es obligatorio.');
      return;
    }

    setIsSaving(true);
    setErrorMsg(null);

    try {
      const created = await api.createJobPosition({
        title: title.trim(),
        department: department.trim(),
        seniority,
        minExperienceYears: Number(minExperienceYears) || 0,
        description: description.trim() || undefined,
        requirements: requirements.trim() || undefined,
      });

      onPositionCreated(created);
      onClose();
      // Reset fields
      setTitle('');
      setDescription('');
      setRequirements('');
    } catch (err: any) {
      console.error('Error al crear vacante:', err);
      setErrorMsg(err.message || 'Error al registrar la nueva vacante.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 bg-slate-950/75 backdrop-blur-xs animate-fade-in"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-lg overflow-hidden flex flex-col h-[95vh] sm:h-auto sm:max-h-[90vh]">
        {/* Header */}
        <div className="px-4 sm:px-6 py-3.5 sm:py-4 bg-slate-900 text-white flex items-center justify-between gap-3">
          <div className="flex items-center space-x-3 min-w-0">
            <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-200 flex-shrink-0">
              <Briefcase className="w-4 h-4 sm:w-5 sm:h-5 text-slate-300" />
            </div>
            <div className="min-w-0">
              <h3 className="text-sm sm:text-base font-bold text-white tracking-tight truncate">
                Nueva Vacante / Convocatoria
              </h3>
              <p className="text-[11px] sm:text-xs text-slate-400 truncate">
                Definición del puesto y criterios de evaluación
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer flex-shrink-0"
            title="Cerrar modal"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-3.5 sm:space-y-4">
          {errorMsg && (
            <div className="p-3 bg-red-50 text-red-700 text-xs sm:text-sm rounded-lg border border-red-200 font-medium">
              {errorMsg}
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Título del Puesto *
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Ej. DevOps & Platform Engineer"
              className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Departamento *
              </label>
              <select
                value={department}
                onChange={(e) => setDepartment(e.target.value)}
                className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 transition-all"
              >
                {DEPARTMENTS.map((d) => (
                  <option key={d} value={d}>
                    {d}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Nivel de Seniority
              </label>
              <select
                value={seniority}
                onChange={(e) => setSeniority(e.target.value)}
                className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 transition-all"
              >
                {SENIORITIES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Años de Experiencia Mínimos
            </label>
            <input
              type="number"
              min={0}
              max={30}
              value={minExperienceYears}
              onChange={(e) => setMinExperienceYears(Number(e.target.value))}
              className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 transition-all"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Requisitos y Competencias Técnicas Clave
            </label>
            <textarea
              rows={3}
              value={requirements}
              onChange={(e) => setRequirements(e.target.value)}
              placeholder="Ej. C#, ASP.NET Core, PostgreSQL, Docker, Microservicios, Arquitectura Limpia."
              className="w-full px-3.5 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 transition-all resize-none"
            />
            <span className="text-[11px] text-slate-500">
              La IA contrastará el CV del postulante contra estas competencias específicas.
            </span>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Descripción de Responsabilidades
            </label>
            <textarea
              rows={2}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Objetivo principal del cargo y ámbito de impacto en la organización."
              className="w-full px-3.5 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 transition-all resize-none"
            />
          </div>
        </form>

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
            onClick={handleSubmit}
            disabled={isSaving}
            className="flex-1 sm:flex-none px-5 py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-semibold rounded-lg shadow-sm transition-all flex items-center justify-center space-x-1.5 disabled:opacity-50 cursor-pointer"
          >
            {isSaving ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle2 className="w-4 h-4 text-emerald-400" />}
            <span>Registrar Vacante</span>
          </button>
        </div>
      </div>
    </div>
  );
};