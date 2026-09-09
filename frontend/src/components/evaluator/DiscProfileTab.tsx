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
  const assessment = candidate.assessmentInterpretation;

  // Determinar tipo de evaluación y estilo dominante
  const primaryStyle = candidate.primaryDiscStyle || assessment?.primaryStyle || disc?.primaryStyle || 'Pendiente de evaluación';

  // Extraer dimensiones dinámicas o reales
  const hasDynamicScores = candidate.assessmentScores && Object.keys(candidate.assessmentScores).length > 0;
  
  const dimensionEntries: [string, number][] = hasDynamicScores
    ? Object.entries(candidate.assessmentScores!)
    : [];

  const dimensionsRecord: Record<string, number> = Object.fromEntries(dimensionEntries);

  // Descripciones de ayuda para dimensiones estándar DISC, Big Five o genéricas
  const getDimensionSubtitle = (name: string): string => {
    const lower = name.toLowerCase();
    if (lower.includes('dominan') || lower === 'd') return 'Orientación a retos, rapidez y metas';
    if (lower.includes('influen') || lower === 'i') return 'Comunicación, persuasión y entusiasmo';
    if (lower.includes('estab') || lower === 's') return 'Paciencia, escucha y trabajo en equipo';
    if (lower.includes('cumplim') || lower === 'c') return 'Rigor técnico, calidad y precisión';
    if (lower.includes('open') || lower.includes('apertur')) return 'Curiosidad intelectual e imaginación';
    if (lower.includes('conscient') || lower.includes('respons')) return 'Autodisciplina y sentido del deber';
    if (lower.includes('extraver')) return 'Sociabilidad, asertividad y energía';
    if (lower.includes('agreeab') || lower.includes('amabil')) return 'Empatía y cooperación interpersonal';
    if (lower.includes('neurotic') || lower.includes('estabil')) return 'Regulación emocional bajo presión';
    return 'Métrica evaluada en el perfil conductual';
  };

  const strengths = assessment?.strengthsToExplore || disc?.strengthsToExplore || [];

  const pointsToExplore = cv?.pointsToValidate || assessment?.pointsToExplore || disc?.pointsToExplore || [];

  return (
    <div className="space-y-6 animate-in fade-in duration-150">
      {/* Assessment Scores & Visualizer */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-5">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <h4 className="text-base font-bold text-slate-900 flex items-center space-x-2">
            <Compass className="w-4 h-4 text-slate-700" />
            <span>Evaluación Conductual {candidate.assessmentType ? `(${candidate.assessmentType})` : 'DISC'}</span>
          </h4>
          <span className="px-3.5 py-1.5 rounded-lg text-xs sm:text-sm font-bold bg-slate-100 text-slate-900 border border-slate-300">
            Estilo Principal: {primaryStyle}
          </span>
        </div>

        {dimensionEntries.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-center">
            <div className="space-y-3 text-sm">
              {dimensionEntries.map(([dimName, dimValue]) => (
                <div key={dimName} className="p-3.5 bg-slate-50 rounded-xl border border-slate-200 flex justify-between items-center">
                  <div>
                    <span className="font-bold text-slate-900 block text-sm">{dimName}</span>
                    <span className="text-xs text-slate-500">{getDimensionSubtitle(dimName)}</span>
                  </div>
                  <span className="text-lg font-black text-slate-900">{Math.round(dimValue)}%</span>
                </div>
              ))}
            </div>

            <div className="flex flex-col items-center justify-center p-4 bg-slate-50 rounded-2xl border border-slate-200">
              <RadarChart
                dimensions={dimensionsRecord}
                size={220}
              />
              <span className="text-xs text-slate-500 mt-2 font-medium">Matriz Conductual de {candidate.firstName}</span>
            </div>
          </div>
        ) : (
          <div className="p-8 text-center bg-slate-50 rounded-xl border border-slate-200 text-slate-500 text-xs">
            <Compass className="w-8 h-8 text-slate-400 mx-auto mb-2" />
            <p className="font-semibold text-slate-700">Sin dimensiones psicométricas registradas</p>
            <p className="mt-0.5">La evaluación conductual de este candidato aún no ha sido cargada en el sistema.</p>
          </div>
        )}
      </div>

      {/* Strengths and Points to Explore */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-white p-5 sm:p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3">
          <h4 className="text-xs sm:text-sm font-bold text-emerald-800 uppercase tracking-wider flex items-center space-x-1.5">
            <TrendingUp className="w-4 h-4 text-emerald-600" />
            <span>Fortalezas Identificadas para el Rol</span>
          </h4>
          {strengths.length > 0 ? (
            <ul className="space-y-2.5 text-xs sm:text-sm text-slate-700">
              {strengths.map((str, i) => (
                <li key={i} className="flex items-start space-x-2.5">
                  <CheckCircle2 className="w-4 h-4 text-emerald-500 flex-shrink-0 mt-0.5" />
                  <span className="leading-relaxed">{str}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-xs text-slate-400 italic">No se han registrado fortalezas conductuales previas.</p>
          )}
        </div>

        <div className="bg-white p-5 sm:p-6 rounded-2xl border border-slate-200 shadow-sm space-y-3">
          <h4 className="text-xs sm:text-sm font-bold text-amber-800 uppercase tracking-wider flex items-center space-x-1.5">
            <AlertTriangle className="w-4 h-4 text-amber-600" />
            <span>Puntos a Explorar en la Entrevista</span>
          </h4>
          {pointsToExplore.length > 0 ? (
            <ul className="space-y-2.5 text-xs sm:text-sm text-slate-700">
              {pointsToExplore.map((pt, i) => (
                <li key={i} className="flex items-start space-x-2.5">
                  <HelpCircle className="w-4 h-4 text-amber-500 flex-shrink-0 mt-0.5" />
                  <span className="leading-relaxed">{pt}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-xs text-slate-400 italic">No se han detectado inconsistencias o puntos a indagar.</p>
          )}
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
