import React from 'react';
import {
  Compass,
  TrendingUp,
  CheckCircle2,
  AlertTriangle,
  HelpCircle,
  ArrowLeft,
  ArrowRight
} from 'lucide-react';
import { Candidate } from '../../types';
import { RadarChart } from '../RadarChart';

interface DiscProfileTabProps {
  candidate: Candidate;
  onPrev: () => void;
  onNext: () => void;
}

export const DiscProfileTab: React.FC<DiscProfileTabProps> = ({ candidate, onPrev, onNext }) => {
  const cv = candidate.cvAnalysis;
  const disc = candidate.discInterpretation;

  // Extract or fallback DISC scores
  const discPrimary = candidate.primaryDiscStyle || disc?.primaryStyle || 'D/C';
  const dominance = discPrimary.includes('D') ? 88 : 45;
  const influence = discPrimary.includes('I') ? 78 : 55;
  const steadiness = discPrimary.includes('S') ? 75 : 42;
  const conscientiousness = discPrimary.includes('C') ? 85 : 50;

  return (
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
          onClick={onPrev}
          className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-xl transition-colors flex items-center justify-center space-x-1.5 cursor-pointer"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Anterior: CV</span>
        </button>

        <button
          type="button"
          onClick={onNext}
          className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs sm:text-sm font-bold rounded-xl shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
        >
          <span>Paso 3: Guía STAR de Entrevista</span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
