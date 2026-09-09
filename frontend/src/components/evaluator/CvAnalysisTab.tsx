import React, { useState, useEffect } from 'react';
import {
  ShieldAlert,
  Mail,
  Phone,
  Calendar,
  Briefcase,
  ArrowRight,
  FileText,
  Sparkles,
  ExternalLink,
  Download,
  Loader2,
  AlertCircle,
  Maximize2
} from 'lucide-react';
import { Candidate } from '../../types';
import { api } from '../../services/api';

interface CvAnalysisTabProps {
  candidate: Candidate;
  onNext: () => void;
  onOpenPdfModal?: () => void;
}

export const CvAnalysisTab: React.FC<CvAnalysisTabProps> = ({
  candidate,
  onNext,
  onOpenPdfModal,
}) => {
  const cv = candidate.cvAnalysis;
  const [viewMode, setViewMode] = useState<'synthesis' | 'pdf'>('synthesis');
  const [pdfBlobUrl, setPdfBlobUrl] = useState<string | null>(null);
  const [isLoadingPdf, setIsLoadingPdf] = useState(false);
  const [pdfError, setPdfError] = useState<string | null>(null);

  // Carga diferida del PDF solo si el usuario selecciona ver el CV original
  useEffect(() => {
    let currentUrl: string | null = null;

    if (viewMode === 'pdf' && !pdfBlobUrl) {
      setIsLoadingPdf(true);
      setPdfError(null);

      api.getCvPdfBlob(candidate.id)
        .then((blob) => {
          const url = window.URL.createObjectURL(blob);
          currentUrl = url;
          setPdfBlobUrl(url);
        })
        .catch((err: any) => {
          setPdfError(err?.message || 'No se pudo cargar el archivo PDF original del postulante.');
        })
        .finally(() => {
          setIsLoadingPdf(false);
        });
    }

    return () => {
      if (currentUrl) {
        window.URL.revokeObjectURL(currentUrl);
      }
    };
  }, [viewMode, candidate.id, pdfBlobUrl]);

  const handleDownloadPdf = () => {
    api.downloadOriginalCv(candidate.id, `cv_${candidate.firstName.toLowerCase()}_${candidate.lastName.toLowerCase()}.pdf`);
  };

  const handleOpenExternal = () => {
    if (pdfBlobUrl) {
      window.open(pdfBlobUrl, '_blank');
    }
  };

  return (
    <div className="space-y-6 animate-in fade-in duration-150">
      {/* Banner Ético de Asistencia Utilitaria (Human-in-the-Loop) */}
      <div className="bg-blue-50/80 border border-blue-200/90 rounded-2xl p-4 shadow-2xs flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-start space-x-3 text-blue-950 text-xs sm:text-sm">
          <div className="p-1.5 rounded-lg bg-blue-100 text-blue-800 flex-shrink-0 mt-0.5">
            <FileText className="w-4 h-4" />
          </div>
          <div>
            <p className="font-bold text-blue-900">
              Herramienta Utilitaria de Apoyo para Selección (Human-in-the-Loop)
            </p>
            <p className="text-xs text-blue-800/90 mt-0.5 leading-relaxed">
              La IA extrae datos objetivos y coteja palabras clave del CV para agilizar la preparación de la entrevista. La lectura del documento original y la valoración del postulante son responsabilidad exclusiva del profesional de Recursos Humanos.
            </p>
          </div>
        </div>

        {/* View Mode Toggle */}
        <div className="flex items-center space-x-1 bg-white p-1 rounded-xl border border-blue-200/80 shadow-2xs self-start sm:self-auto flex-shrink-0">
          <button
            type="button"
            onClick={() => setViewMode('synthesis')}
            className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all flex items-center space-x-1.5 cursor-pointer ${
              viewMode === 'synthesis'
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Sparkles className="w-3.5 h-3.5" />
            <span>Síntesis Asistida</span>
          </button>
          <button
            type="button"
            onClick={() => setViewMode('pdf')}
            className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all flex items-center space-x-1.5 cursor-pointer ${
              viewMode === 'pdf'
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <FileText className="w-3.5 h-3.5 text-blue-500" />
            <span>Ver CV Original (PDF)</span>
          </button>
        </div>
      </div>

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

      {/* Contact Overview & Requirements Match */}
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

        <div className="flex items-center space-x-2.5 flex-shrink-0 self-start md:self-auto">
          <span className="text-xs sm:text-sm text-slate-500 font-medium">Cotejo de Requisitos:</span>
          <span
            className="px-3 py-1 bg-emerald-50 text-emerald-800 text-xs sm:text-sm font-bold rounded-lg border border-emerald-200"
            title="Correspondencia informativa de tecnologías y experiencia declaradas en CV frente al puesto. No representa una calificación."
          >
            {candidate.matchScore ?? 0}% detectado con {candidate.targetRole}
          </span>
        </div>
      </div>

      {/* VISTA 1: SÍNTESIS ASISTIDA DE EXTRACCIÓN */}
      {viewMode === 'synthesis' && (
        <div className="bg-white p-4 sm:p-6 rounded-2xl border border-slate-200 shadow-sm space-y-5">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-100 pb-3 gap-2">
            <h4 className="text-sm sm:text-base font-bold text-slate-900 flex items-center space-x-2">
              <Briefcase className="w-4 h-4 text-slate-700 flex-shrink-0" />
              <span>Síntesis Curricular & Evidencias Detectadas</span>
            </h4>
            <div className="flex items-center space-x-2">
              <span className="text-xs sm:text-sm text-slate-600">
                Seniority: <strong>{candidate.seniority}</strong> ({candidate.experienceYears} años declarados)
              </span>
              <button
                type="button"
                onClick={() => setViewMode('pdf')}
                className="text-xs font-semibold text-blue-600 hover:text-blue-800 underline ml-2 cursor-pointer"
              >
                Ver CV original
              </button>
            </div>
          </div>

          {cv ? (
            <div className="space-y-6 text-sm">
              <div>
                <p className="font-bold text-slate-700 mb-2 uppercase tracking-wider text-xs">
                  Resumen de Trayectoria Extraído:
                </p>
                <p className="p-3.5 sm:p-4 bg-slate-50 rounded-xl border border-slate-200 text-slate-700 leading-relaxed font-normal text-xs sm:text-sm">
                  {cv.professionalSummary}
                </p>
              </div>

              {/* Technical Skills */}
              <div>
                <p className="font-bold text-slate-700 mb-2.5 uppercase tracking-wider text-xs">
                  Habilidades Técnicas y Evidencia Textual en el Documento:
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
                    Experiencia Laboral Cronológica:
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
      )}

      {/* VISTA 2: VISOR WEB DIRECTO DEL CV ORIGINAL (PDF) */}
      {viewMode === 'pdf' && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden flex flex-col">
          <div className="px-4 py-3 bg-slate-900 text-white flex items-center justify-between gap-3">
            <div className="flex items-center space-x-2">
              <FileText className="w-4 h-4 text-blue-400" />
              <span className="text-xs sm:text-sm font-bold">
                Currículum Vítae Original — {candidate.firstName} {candidate.lastName}
              </span>
            </div>
            <div className="flex items-center space-x-2">
              {onOpenPdfModal && (
                <button
                  type="button"
                  onClick={onOpenPdfModal}
                  className="px-2.5 py-1 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-xs font-medium transition-colors flex items-center space-x-1 cursor-pointer"
                  title="Expandir a pantalla completa"
                >
                  <Maximize2 className="w-3.5 h-3.5" />
                  <span className="hidden sm:inline">Pantalla Completa</span>
                </button>
              )}
              {pdfBlobUrl && (
                <button
                  type="button"
                  onClick={handleOpenExternal}
                  className="px-2.5 py-1 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-xs font-medium transition-colors flex items-center space-x-1 cursor-pointer"
                  title="Abrir en pestaña nueva"
                >
                  <ExternalLink className="w-3.5 h-3.5" />
                  <span className="hidden sm:inline">Nueva Pestaña</span>
                </button>
              )}
              <button
                type="button"
                onClick={handleDownloadPdf}
                className="px-2.5 py-1 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-xs font-medium transition-colors flex items-center space-x-1 cursor-pointer"
                title="Descargar PDF original"
              >
                <Download className="w-3.5 h-3.5" />
                <span className="hidden sm:inline">Descargar</span>
              </button>
            </div>
          </div>

          <div className="h-[650px] bg-slate-100 flex items-center justify-center relative">
            {isLoadingPdf && (
              <div className="flex flex-col items-center justify-center space-y-2 text-slate-600">
                <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                <p className="text-xs font-semibold">Cargando PDF original en el visor...</p>
              </div>
            )}

            {!isLoadingPdf && pdfError && (
              <div className="flex flex-col items-center justify-center space-y-2 p-6 text-center max-w-md">
                <AlertCircle className="w-8 h-8 text-red-500" />
                <p className="text-xs font-bold text-slate-900">No se pudo cargar el PDF</p>
                <p className="text-xs text-slate-500">{pdfError}</p>
                <button
                  type="button"
                  onClick={() => setViewMode('synthesis')}
                  className="mt-2 text-xs font-semibold text-blue-600 underline"
                >
                  Volver a la síntesis
                </button>
              </div>
            )}

            {!isLoadingPdf && !pdfError && pdfBlobUrl && (
              <iframe
                src={pdfBlobUrl}
                title={`CV original de ${candidate.firstName} ${candidate.lastName}`}
                className="w-full h-full border-0 bg-white"
              />
            )}
          </div>
        </div>
      )}

      {/* Step Navigation Button */}
      <div className="flex items-center justify-between pt-2">
        <div className="text-xs text-slate-500 italic">
          Paso 1 de 5 del Expediente de Selección
        </div>
        <button
          type="button"
          onClick={onNext}
          className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-sm font-bold rounded-xl shadow-sm transition-all flex items-center space-x-2 cursor-pointer"
        >
          <span>Paso 2: Perfil Psicométrico</span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
