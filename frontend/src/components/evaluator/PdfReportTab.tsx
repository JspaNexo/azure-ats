import React, { useState, useEffect, useRef } from 'react';
import {
  Loader2,
  Download,
  ArrowLeft,
  FileText,
  ExternalLink,
  AlertCircle,
  LayoutGrid,
  Eye
} from 'lucide-react';
import { Candidate } from '../../types';
import { api } from '../../services/api';

interface PdfReportTabProps {
  candidate: Candidate;
  isDownloading: boolean;
  onDownloadPdf: () => void;
  onPrev: () => void;
  onClose: () => void;
}

export const PdfReportTab: React.FC<PdfReportTabProps> = ({
  candidate,
  isDownloading,
  onDownloadPdf,
  onPrev,
  onClose,
}) => {
  const blobUrlRef = useRef<string | null>(null);
  const [viewMode, setViewMode] = useState<'pdf' | 'summary'>('pdf');
  const [pdfBlobUrl, setPdfBlobUrl] = useState<string | null>(null);
  const [isLoadingPdf, setIsLoadingPdf] = useState(false);
  const [pdfError, setPdfError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    setIsLoadingPdf(true);
    setPdfError(null);

    api.getReportPdfBlob(candidate.id)
      .then((blob) => {
        if (!isMounted) return;
        const url = window.URL.createObjectURL(blob);
        blobUrlRef.current = url;
        setPdfBlobUrl(url);
      })
      .catch((err: any) => {
        if (!isMounted) return;
        console.error('Error cargando PDF del informe:', err);
        setPdfError(err?.message || 'No se pudo generar o cargar la previsualización del expediente pre-entrevista.');
      })
      .finally(() => {
        if (isMounted) {
          setIsLoadingPdf(false);
        }
      });

    return () => {
      isMounted = false;
      if (blobUrlRef.current) {
        window.URL.revokeObjectURL(blobUrlRef.current);
        blobUrlRef.current = null;
      }
    };
  }, [candidate.id]);

  const handleOpenExternal = () => {
    if (pdfBlobUrl) {
      window.open(pdfBlobUrl, '_blank');
    }
  };

  return (
    <div className="space-y-6 animate-in fade-in duration-150">
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-5 sm:p-6 space-y-5">
        {/* Header Bar with Controls */}
        <div className="border-b border-slate-200 pb-4 flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div>
            <div className="flex items-center space-x-2">
              <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
                Expediente Oficial Pre-Entrevista • Formato Auditado
              </span>
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-blue-50 text-blue-800 border border-blue-200">
                Visor Web Nativo
              </span>
            </div>
            <h3 className="text-base font-bold text-slate-900 mt-0.5">
              Expediente Completo de Selección: {candidate.firstName} {candidate.lastName}
            </h3>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {/* View Mode Toggle */}
            <div className="flex items-center bg-slate-100 p-1 rounded-xl border border-slate-200 text-xs">
              <button
                type="button"
                onClick={() => setViewMode('pdf')}
                className={`px-3 py-1.5 rounded-lg font-bold transition-all flex items-center space-x-1.5 cursor-pointer ${
                  viewMode === 'pdf'
                    ? 'bg-slate-900 text-white shadow-xs'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <Eye className="w-3.5 h-3.5" />
                <span>Visor Documento (PDF)</span>
              </button>
              <button
                type="button"
                onClick={() => setViewMode('summary')}
                className={`px-3 py-1.5 rounded-lg font-bold transition-all flex items-center space-x-1.5 cursor-pointer ${
                  viewMode === 'summary'
                    ? 'bg-slate-900 text-white shadow-xs'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <LayoutGrid className="w-3.5 h-3.5" />
                <span>Resumen de Folios</span>
              </button>
            </div>

            {/* External New Tab Button */}
            {pdfBlobUrl && (
              <button
                type="button"
                onClick={handleOpenExternal}
                className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg border border-slate-200 transition-colors flex items-center space-x-1.5 cursor-pointer"
                title="Abrir PDF en pestaña independiente del navegador"
              >
                <ExternalLink className="w-3.5 h-3.5 text-slate-500" />
                <span className="hidden sm:inline">Pestaña Nueva</span>
              </button>
            )}

            {/* Direct Download Button */}
            <button
              type="button"
              onClick={onDownloadPdf}
              disabled={isDownloading}
              className="flex items-center justify-center space-x-2 px-4 py-1.5 sm:py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-lg shadow-sm transition-all disabled:opacity-50 cursor-pointer"
            >
              {isDownloading ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <Download className="w-3.5 h-3.5" />
              )}
              <span>Descargar PDF</span>
            </button>
          </div>
        </div>

        {/* VISTA 1: VISOR INTERACTIVO EN VIVO DEL PDF */}
        {viewMode === 'pdf' && (
          <div className="rounded-xl border border-slate-200 overflow-hidden bg-slate-100 relative min-h-[620px] flex items-center justify-center">
            {isLoadingPdf && (
              <div className="flex flex-col items-center justify-center space-y-2.5 p-8 text-slate-600">
                <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                <p className="text-xs font-bold text-slate-800">Generando previsualización del informe...</p>
                <p className="text-[11px] text-slate-500">Compilando perfil curricular, matriz conductual y protocolo STAR</p>
              </div>
            )}

            {!isLoadingPdf && pdfError && (
              <div className="flex flex-col items-center justify-center space-y-3 p-8 text-center max-w-md">
                <AlertCircle className="w-9 h-9 text-red-500" />
                <div>
                  <h4 className="text-sm font-bold text-slate-900">No se pudo cargar el visor del PDF</h4>
                  <p className="text-xs text-slate-500 mt-1">{pdfError}</p>
                </div>
                <div className="flex items-center space-x-3 pt-2">
                  <button
                    type="button"
                    onClick={() => setViewMode('summary')}
                    className="px-3 py-1.5 bg-slate-200 hover:bg-slate-300 text-slate-800 text-xs font-semibold rounded-lg"
                  >
                    Ver Resumen Estructurado
                  </button>
                  <button
                    type="button"
                    onClick={onDownloadPdf}
                    className="px-3 py-1.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-semibold rounded-lg flex items-center space-x-1"
                  >
                    <Download className="w-3.5 h-3.5" />
                    <span>Intentar Descarga</span>
                  </button>
                </div>
              </div>
            )}

            {!isLoadingPdf && !pdfError && pdfBlobUrl && (
              <iframe
                src={pdfBlobUrl}
                title={`Expediente Oficial de ${candidate.firstName} ${candidate.lastName}`}
                className="w-full h-[650px] sm:h-[720px] border-0 bg-white"
              />
            )}
          </div>
        )}

        {/* VISTA 2: RESUMEN ESTRUCTURADO DE FOLIOS */}
        {viewMode === 'summary' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5 animate-in fade-in duration-100">
            <div className="p-5 bg-slate-50 rounded-2xl border border-slate-200 space-y-2 text-xs">
              <div className="flex justify-between items-center border-b border-slate-200 pb-2">
                <span className="font-bold text-slate-900 flex items-center space-x-1.5">
                  <FileText className="w-4 h-4 text-blue-600" />
                  <span>Página 1: Resumen Curricular & Matriz Conductual</span>
                </span>
                <span className="text-[10px] px-2 py-0.5 bg-slate-200 text-slate-800 font-bold rounded">Folio 1</span>
              </div>
              <div className="text-slate-600 space-y-1.5 pt-1">
                <p><strong>Candidato:</strong> {candidate.firstName} {candidate.lastName}</p>
                <p><strong>Posición Evaluada:</strong> {candidate.targetRole} {candidate.seniority ? `(${candidate.seniority})` : ''}</p>
                <p><strong>Cotejo de Requisitos:</strong> {candidate.matchScore > 0 ? `${candidate.matchScore}% detectado` : 'Sin requisitos contrastados'}</p>
                <p><strong>Estilo Conductual:</strong> {candidate.primaryDiscStyle ? `Patrón ${candidate.primaryDiscStyle}` : 'Sin evaluar'}</p>
                <p><strong>Años de Experiencia:</strong> {candidate.experienceYears > 0 ? `${candidate.experienceYears} años acreditados` : 'Por determinar'}</p>
              </div>
            </div>

            <div className="p-5 bg-slate-50 rounded-2xl border border-slate-200 space-y-2 text-xs">
              <div className="flex justify-between items-center border-b border-slate-200 pb-2">
                <span className="font-bold text-slate-900 flex items-center space-x-1.5">
                  <FileText className="w-4 h-4 text-emerald-600" />
                  <span>Página 2: Protocolo STAR & Dictamen Soberano</span>
                </span>
                <span className="text-[10px] px-2 py-0.5 bg-slate-200 text-slate-800 font-bold rounded">Folio 2</span>
              </div>
              <div className="text-slate-600 space-y-1.5 pt-1">
                <p><strong>Preguntas Situacionales:</strong> Metodología STAR personalizada por vacante</p>
                <p><strong>Dictamen del Evaluador:</strong> {candidate.evaluatorDecision || 'Pendiente de resolución'}</p>
                <p><strong>Evaluador Designado:</strong> {candidate.assignedRecruiterName || 'Comité de Selección'}</p>
                <p><strong>Integridad del Expediente:</strong> Foliado digital y trazabilidad auditada</p>
              </div>
            </div>
          </div>
        )}

        {/* Banner Ético de Human-in-the-Loop */}
        <div className="p-3.5 bg-blue-50/80 border border-blue-200/90 rounded-xl text-xs text-blue-900 leading-relaxed">
          <span className="font-bold">Aviso Ético de Selección (Human-in-the-Loop): </span>
          <span>
            Este expediente pre-entrevista es un dossier utilitario generado mediante IA para apoyar la lectura rápida y estructurar la entrevista. No constituye una evaluación de aptitud ni una decisión vinculante. La revisión directa del currículum original y la resolución final son competencia soberana y exclusiva del profesional de Recursos Humanos.
          </span>
        </div>
      </div>

      {/* Step Navigation Buttons */}
      <div className="flex items-center justify-between pt-2">
        <button
          type="button"
          onClick={onPrev}
          className="px-4 py-2.5 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs font-semibold rounded-xl transition-colors flex items-center space-x-1.5 cursor-pointer"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Anterior: Dictamen Oficial</span>
        </button>

        <button
          type="button"
          onClick={onClose}
          className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl shadow-sm transition-all cursor-pointer"
        >
          <span>Concluir y Cerrar Expediente</span>
        </button>
      </div>
    </div>
  );
};
