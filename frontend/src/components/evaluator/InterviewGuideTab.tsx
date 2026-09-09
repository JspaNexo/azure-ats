import React from 'react';
import {
  Award,
  Compass,
  Briefcase,
  ArrowLeft,
  ArrowRight
} from 'lucide-react';
import { Candidate } from '../../types';

interface InterviewGuideTabProps {
  candidate: Candidate;
  onPrev: () => void;
  onNext: () => void;
}

export const InterviewGuideTab: React.FC<InterviewGuideTabProps> = ({ candidate, onPrev, onNext }) => {
  const report = candidate.report;
  const techQuestions = report?.interviewGuide?.technicalQuestions || [];
  const behavioralQuestions = report?.interviewGuide?.behavioralQuestions || [];
  const professionalQuestions = report?.interviewGuide?.professionalQuestions || [];
  const hasAnyQuestions = techQuestions.length > 0 || behavioralQuestions.length > 0 || professionalQuestions.length > 0;

  return (
    <div className="space-y-6 animate-in fade-in duration-150">
      <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h4 className="text-base font-bold text-slate-900">Protocolo de Entrevista Estructurada (Metodología STAR)</h4>
          <p className="text-xs sm:text-sm text-slate-600 mt-0.5">
            Preguntas de indagación preparadas según el perfil de {candidate.firstName} {candidate.targetRole ? `para el rol de ${candidate.targetRole}` : ''}.
          </p>
        </div>
        <span className={`px-3.5 py-1 text-xs font-bold rounded-xl flex-shrink-0 self-start sm:self-auto ${
          hasAnyQuestions ? 'bg-slate-900 text-white' : 'bg-amber-100 text-amber-800'
        }`}>
          {hasAnyQuestions ? 'Guía Disponible' : 'Pendiente de Generación'}
        </span>
      </div>

      {!hasAnyQuestions && (
        <div className="p-6 bg-slate-50 border border-slate-200 rounded-2xl text-center space-y-2">
          <p className="text-sm font-bold text-slate-800">Guía de Entrevista no disponible</p>
          <p className="text-xs text-slate-500 max-w-lg mx-auto">
            El informe pre-entrevista estructurado con metodología STAR para este postulante aún no ha sido emitido. Las preguntas personalizadas se generan a partir del análisis del CV y la vacante asignada.
          </p>
        </div>
      )}

      {/* Technical Questions */}
      {techQuestions.length > 0 && (
        <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3.5">
          <h4 className="text-xs sm:text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center space-x-2">
            <Award className="w-4 h-4 text-blue-600" />
            <span>1. Indagación Técnica Específica ({candidate.targetRole || 'Especialidad'})</span>
          </h4>
          <div className="space-y-3 text-sm">
            {techQuestions.map((q, i) => (
              <div key={i} className="p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium flex items-start space-x-3 text-sm sm:text-base">
                <span className="w-6 h-6 rounded-full bg-slate-200 text-slate-800 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                  {i + 1}
                </span>
                <span className="leading-relaxed">{q}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Behavioral Questions */}
      {behavioralQuestions.length > 0 && (
        <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3.5">
          <h4 className="text-xs sm:text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center space-x-2">
            <Compass className="w-4 h-4 text-emerald-600" />
            <span>2. Competencias Conductuales & Gestión Situacional (STAR)</span>
          </h4>
          <div className="space-y-3 text-sm">
            {behavioralQuestions.map((q, i) => (
              <div key={i} className="p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium flex items-start space-x-3 text-sm sm:text-base">
                <span className="w-6 h-6 rounded-full bg-slate-200 text-slate-800 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                  {i + 1}
                </span>
                <span className="leading-relaxed">{q}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Professional Questions */}
      {professionalQuestions.length > 0 && (
        <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3.5">
          <h4 className="text-xs sm:text-sm font-bold text-slate-900 uppercase tracking-wider flex items-center space-x-2">
            <Briefcase className="w-4 h-4 text-purple-600" />
            <span>3. Trayectoria Laboral & Aprendizajes Clave</span>
          </h4>
          <div className="space-y-3 text-sm">
            {professionalQuestions.map((q, i) => (
              <div key={i} className="p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-800 font-medium flex items-start space-x-3 text-sm sm:text-base">
                <span className="w-6 h-6 rounded-full bg-slate-200 text-slate-800 font-bold text-xs flex items-center justify-center flex-shrink-0 mt-0.5">
                  {i + 1}
                </span>
                <span className="leading-relaxed">{q}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Step Navigation Buttons */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5 pt-2">
        <button
          type="button"
          onClick={onPrev}
          className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-xl transition-colors flex items-center justify-center space-x-1.5 cursor-pointer"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Anterior: DISC</span>
        </button>

        <button
          type="button"
          onClick={onNext}
          className="px-5 py-2.5 bg-emerald-700 hover:bg-emerald-800 text-white text-xs sm:text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
        >
          <span>Paso 4: Registrar Dictamen Oficial</span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
