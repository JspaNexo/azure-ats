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

  return (
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
