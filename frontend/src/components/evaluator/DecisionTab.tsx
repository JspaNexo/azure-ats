import React from 'react';
import {
  FileCheck,
  CheckCircle2,
  Clock,
  XCircle,
  HelpCircle,
  Loader2,
  Save,
  ArrowLeft,
  ArrowRight
} from 'lucide-react';
import { Candidate } from '../../types';

export const decisionOptions = [
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

interface DecisionTabProps {
  candidate: Candidate;
  decision: string;
  setDecision: (decision: string) => void;
  notes: string;
  setNotes: (notes: string) => void;
  decisionSuccessMsg: string | null;
  isSavingDecision: boolean;
  onSaveDecision: () => void;
  onPrev: () => void;
  onNext: () => void;
}

export const DecisionTab: React.FC<DecisionTabProps> = ({
  candidate,
  decision,
  setDecision,
  notes,
  setNotes,
  decisionSuccessMsg,
  isSavingDecision,
  onSaveDecision,
  onPrev,
  onNext,
}) => {
  return (
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
            onClick={onSaveDecision}
            disabled={isSavingDecision}
            className="w-full sm:w-auto px-6 py-3 bg-slate-900 hover:bg-slate-800 active:scale-95 text-white text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 disabled:opacity-50 cursor-pointer"
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
          onClick={onPrev}
          className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-xl transition-colors flex items-center justify-center space-x-1.5 cursor-pointer"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Anterior: Preguntas STAR</span>
        </button>

        <button
          type="button"
          onClick={onNext}
          className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
        >
          <span>Paso 5: Ver Documento Oficial PDF</span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
