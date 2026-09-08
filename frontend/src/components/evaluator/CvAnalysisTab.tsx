import React from 'react';
import {
  ShieldAlert,
  Mail,
  Phone,
  Calendar,
  Briefcase,
  ArrowRight
} from 'lucide-react';
import { Candidate } from '../../types';

interface CvAnalysisTabProps {
  candidate: Candidate;
  onNext: () => void;
}

export const CvAnalysisTab: React.FC<CvAnalysisTabProps> = ({ candidate, onNext }) => {
  const cv = candidate.cvAnalysis;

  return (
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
          onClick={onNext}
          className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-sm font-bold rounded-xl shadow-sm transition-all flex items-center space-x-2 cursor-pointer"
        >
          <span>Paso 2: Revisar Perfil DISC</span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
