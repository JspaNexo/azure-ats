import React from 'react';
import {
  Loader2,
  Download,
  ArrowLeft
} from 'lucide-react';
import { Candidate } from '../../types';

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
  return (
    <div className="space-y-6 animate-in fade-in duration-150">
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-6 space-y-6">
        <div className="border-b border-slate-200 pb-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
              Expediente Oficial de Selección • Formato Auditado
            </span>
            <h3 className="text-base font-bold text-slate-900">
              Previsualización y Descarga del Documento
            </h3>
          </div>
          <button
            type="button"
            onClick={onDownloadPdf}
            disabled={isDownloading}
            className="flex items-center justify-center space-x-2 px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl shadow-sm transition-all disabled:opacity-50 cursor-pointer"
          >
            {isDownloading ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Download className="w-4 h-4" />
            )}
            <span>Descargar Expediente PDF</span>
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          <div className="p-5 bg-slate-50 rounded-2xl border border-slate-200 space-y-2 text-xs">
            <div className="flex justify-between items-center border-b border-slate-200 pb-2">
              <span className="font-bold text-slate-900">Página 1: Resumen Curricular & DISC</span>
              <span className="text-[10px] px-2 py-0.5 bg-slate-200 text-slate-800 font-bold rounded">Folio 1</span>
            </div>
            <div className="text-slate-600 space-y-1.5 pt-1">
              <p><strong>Candidato:</strong> {candidate.firstName} {candidate.lastName}</p>
              <p><strong>Posición:</strong> {candidate.targetRole} ({candidate.seniority})</p>
              <p><strong>Compatibilidad:</strong> {candidate.matchScore}%</p>
              <p><strong>Estilo Conductual:</strong> Patrón DISC {candidate.primaryDiscStyle}</p>
            </div>
          </div>

          <div className="p-5 bg-slate-50 rounded-2xl border border-slate-200 space-y-2 text-xs">
            <div className="flex justify-between items-center border-b border-slate-200 pb-2">
              <span className="font-bold text-slate-900">Página 2: Protocolo STAR & Resolución</span>
              <span className="text-[10px] px-2 py-0.5 bg-slate-200 text-slate-800 font-bold rounded">Folio 2</span>
            </div>
            <div className="text-slate-600 space-y-1.5 pt-1">
              <p><strong>Preguntas Situacionales:</strong> Metodología STAR integrada</p>
              <p><strong>Resolución Registrada:</strong> {candidate.evaluatorDecision || 'Pendiente'}</p>
              <p><strong>Evaluador Asignado:</strong> {candidate.assignedRecruiterName || 'Comité de Selección'}</p>
              <p><strong>Estado del Documento:</strong> Firmado digitalmente y archivado</p>
            </div>
          </div>
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
