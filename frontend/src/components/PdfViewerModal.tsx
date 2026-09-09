import React, { useState, useEffect } from 'react';
import { X, Download, Loader2, AlertCircle, FileText, ExternalLink } from 'lucide-react';
import { api } from '../services/api';

interface PdfViewerModalProps {
  isOpen: boolean;
  candidateId: string | null;
  candidateName: string;
  onClose: () => void;
}

export const PdfViewerModal: React.FC<PdfViewerModalProps> = ({
  isOpen,
  candidateId,
  candidateName,
  onClose,
}) => {
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let currentUrl: string | null = null;

    if (isOpen && candidateId) {
      setIsLoading(true);
      setError(null);

      api.getCvPdfBlob(candidateId)
        .then((blob) => {
          const url = window.URL.createObjectURL(blob);
          currentUrl = url;
          setBlobUrl(url);
        })
        .catch((err: any) => {
          setError(err?.message || 'No se pudo cargar el documento curricular original.');
        })
        .finally(() => {
          setIsLoading(false);
        });
    }

    return () => {
      if (currentUrl) {
        window.URL.revokeObjectURL(currentUrl);
      }
      setBlobUrl(null);
      setError(null);
    };
  }, [isOpen, candidateId]);

  // Cerrar con tecla Escape
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleDownload = () => {
    if (!candidateId) return;
    api.downloadOriginalCv(candidateId, `cv_${candidateName.toLowerCase().replace(/\s+/g, '_')}.pdf`);
  };

  const handleOpenNewTab = () => {
    if (blobUrl) {
      window.open(blobUrl, '_blank');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 md:p-6 bg-slate-950/85 backdrop-blur-xs animate-fade-in"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-300 w-full max-w-5xl h-[92vh] flex flex-col overflow-hidden">
        {/* Header Bar */}
        <div className="px-4 sm:px-6 py-3.5 bg-slate-900 text-white border-b border-slate-800 flex items-center justify-between gap-3">
          <div className="flex items-center space-x-3 min-w-0 flex-1">
            <div className="w-9 h-9 rounded-xl bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-300 flex-shrink-0">
              <FileText className="w-5 h-5 text-blue-400" />
            </div>
            <div className="min-w-0 flex-1">
              <div className="flex items-center space-x-2">
                <h3 className="text-sm sm:text-base font-bold text-white tracking-tight truncate">
                  Currículum Vítae Original
                </h3>
                <span className="hidden sm:inline-block px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-slate-800 text-slate-300 border border-slate-700">
                  Documento Fuente
                </span>
              </div>
              <p className="text-xs text-slate-400 truncate">
                Postulante: <strong className="text-slate-200">{candidateName}</strong>
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2 flex-shrink-0">
            {blobUrl && (
              <>
                <button
                  type="button"
                  onClick={handleOpenNewTab}
                  className="hidden sm:flex items-center space-x-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold rounded-lg border border-slate-700 transition-colors cursor-pointer"
                  title="Abrir en pestaña nueva"
                >
                  <ExternalLink className="w-3.5 h-3.5" />
                  <span>Nueva Pestaña</span>
                </button>

                <button
                  type="button"
                  onClick={handleDownload}
                  className="flex items-center space-x-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold rounded-lg border border-slate-700 transition-colors cursor-pointer"
                  title="Descargar copia del PDF"
                >
                  <Download className="w-3.5 h-3.5" />
                  <span className="hidden sm:inline">Descargar</span>
                </button>
              </>
            )}

            <button
              type="button"
              onClick={onClose}
              className="p-1.5 sm:p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
              title="Cerrar visor (Esc)"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Content Viewer */}
        <div className="flex-1 bg-slate-100 flex items-center justify-center relative overflow-hidden">
          {isLoading && (
            <div className="flex flex-col items-center justify-center space-y-3 p-8 text-center">
              <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
              <p className="text-sm font-semibold text-slate-700">
                Cargando documento curricular original...
              </p>
              <p className="text-xs text-slate-400">
                Obteniendo archivo PDF seguro desde el repositorio
              </p>
            </div>
          )}

          {!isLoading && error && (
            <div className="flex flex-col items-center justify-center space-y-3 p-8 text-center max-w-md">
              <div className="w-12 h-12 rounded-full bg-red-50 text-red-600 flex items-center justify-center border border-red-200">
                <AlertCircle className="w-6 h-6" />
              </div>
              <h4 className="text-base font-bold text-slate-900">Documento no disponible</h4>
              <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">{error}</p>
              <button
                type="button"
                onClick={onClose}
                className="mt-2 px-4 py-2 bg-slate-900 text-white text-xs font-semibold rounded-lg hover:bg-slate-800 transition-colors"
              >
                Cerrar Visor
              </button>
            </div>
          )}

          {!isLoading && !error && blobUrl && (
            <iframe
              src={blobUrl}
              title={`Currículum Vítae de ${candidateName}`}
              className="w-full h-full border-0 bg-white"
            />
          )}
        </div>
      </div>
    </div>
  );
};

