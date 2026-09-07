import React, { useState, useEffect, useRef } from 'react';
import {
  X,
  UploadCloud,
  FileText,
  CheckCircle2,
  AlertCircle,
  Sparkles,
  SlidersHorizontal,
  Briefcase,
  User,
  RotateCcw
} from 'lucide-react';
import { Candidate, JobPosition } from '../types';
import { api } from '../services/api';

interface UploadCandidateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCandidateIngested: (candidate: Candidate) => void;
}

export const UploadCandidateModal: React.FC<UploadCandidateModalProps> = ({
  isOpen,
  onClose,
  onCandidateIngested,
}) => {
  const [positions, setPositions] = useState<JobPosition[]>([]);
  const [selectedPositionId, setSelectedPositionId] = useState<string>('');
  const [customRole, setCustomRole] = useState<string>('');

  // Personal Info
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');

  // CV File
  const [file, setFile] = useState<File | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  // DISC Parameters
  const [dominance, setDominance] = useState<number>(75);
  const [influence, setInfluence] = useState<number>(65);
  const [steadiness, setSteadiness] = useState<number>(45);
  const [conscientiousness, setConscientiousness] = useState<number>(80);

  // Process status
  const [isProcessing, setIsProcessing] = useState(false);
  const [processingStep, setProcessingStep] = useState<string>('');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      loadPositions();
    }
  }, [isOpen]);

  const loadPositions = async () => {
    try {
      const data = await api.getJobPositions('Active');
      setPositions(data);
      if (data.length > 0 && !selectedPositionId) {
        setSelectedPositionId(data[0].id);
      }
    } catch (err) {
      console.error('Error al cargar vacantes activas:', err);
    }
  };

  if (!isOpen) return null;

  // Calculate primary DISC style
  const getPrimaryStyle = () => {
    const scores = [
      { key: 'D', val: dominance },
      { key: 'I', val: influence },
      { key: 'S', val: steadiness },
      { key: 'C', val: conscientiousness },
    ].sort((a, b) => b.val - a.val);

    return `${scores[0].key}/${scores[1].key}`;
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      validateAndSetFile(e.target.files[0]);
    }
  };

  const validateAndSetFile = (selectedFile: File) => {
    setErrorMsg(null);
    if (!selectedFile.name.toLowerCase().endsWith('.pdf')) {
      setErrorMsg('El archivo debe estar en formato PDF.');
      return;
    }
    if (selectedFile.size > 15 * 1024 * 1024) {
      setErrorMsg('El archivo excede el límite máximo de 15 MB.');
      return;
    }
    setFile(selectedFile);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      validateAndSetFile(e.dataTransfer.files[0]);
    }
  };

  const setAverageDisc = () => {
    setDominance(50);
    setInfluence(50);
    setSteadiness(50);
    setConscientiousness(50);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setErrorMsg('Debe adjuntar el archivo CV en PDF del candidato.');
      return;
    }
    if (!firstName.trim() || !lastName.trim() || !email.trim()) {
      setErrorMsg('Nombre, apellido y correo electrónico son obligatorios.');
      return;
    }

    const trimmedEmail = email.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(trimmedEmail)) {
      setErrorMsg('Por favor, ingrese un correo electrónico válido (ejemplo: candidato@empresa.com).');
      return;
    }

    setIsProcessing(true);
    setErrorMsg(null);
    setProcessingStep('Iniciando carga y validación de seguridad...');

    const formData = new FormData();
    formData.append('FirstName', firstName.trim());
    formData.append('LastName', lastName.trim());
    formData.append('Email', trimmedEmail);
    if (phone.trim()) formData.append('PhoneNumber', phone.trim());

    // Position / Target Role
    if (selectedPositionId && selectedPositionId !== 'OTHER') {
      formData.append('JobPositionId', selectedPositionId);
      const pos = positions.find((p) => p.id === selectedPositionId);
      if (pos) formData.append('TargetRole', pos.title);
    } else if (customRole.trim()) {
      formData.append('TargetRole', customRole.trim());
    }

    formData.append('File', file);
    formData.append('Dominance', dominance.toString());
    formData.append('Influence', influence.toString());
    formData.append('Steadiness', steadiness.toString());
    formData.append('Conscientiousness', conscientiousness.toString());
    formData.append('PrimaryStyle', getPrimaryStyle());

    try {
      // Step simulation for transparency
      setTimeout(() => {
        setProcessingStep('Extrayendo texto y aplicando defensas anti-injection...');
      }, 1000);

      setTimeout(() => {
        setProcessingStep('Analizando experiencia y habilidades con Google Gemini AI...');
      }, 2500);

      setTimeout(() => {
        setProcessingStep('Contrastando con vacante y generando preguntas STAR...');
      }, 5000);

      const candidate = await api.ingestCandidate(formData);

      onCandidateIngested(candidate);
      onClose();
    } catch (err: any) {
      console.error('Error durante la ingesta:', err);
      let message = err.message || 'Error al procesar el expediente con IA. Verifique los datos ingresados.';
      if (message.includes('Email.InvalidFormat')) {
        message = 'El formato del correo electrónico no es válido. Ingrese una dirección completa (ej. nombre@dominio.com).';
      } else if (message.includes('Quota exceeded') || message.includes('429') || message.includes('RESOURCE_EXHAUSTED')) {
        message = 'Se alcanzó temporalmente el límite de cuota de la IA. Por favor, reintente en unos momentos.';
      } else if (message.includes('Gemini.ApiError')) {
        message = 'El servicio de IA experimentó una interrupción temporal. Por favor, reintente en unos instantes.';
      }
      setErrorMsg(message);
    } finally {
      setIsProcessing(false);
      setProcessingStep('');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 bg-slate-950/75 backdrop-blur-xs animate-fade-in"
      onClick={(e) => !isProcessing && e.target === e.currentTarget && onClose()}
    >
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-2xl overflow-hidden flex flex-col h-[95vh] sm:h-auto sm:max-h-[92vh]">
        {/* Header */}
        <div className="px-4 sm:px-6 py-3.5 sm:py-4 bg-slate-900 text-white flex items-center justify-between gap-3">
          <div className="flex items-center space-x-3 min-w-0">
            <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-200 flex-shrink-0">
              <UploadCloud className="w-4 h-4 sm:w-5 sm:h-5 text-emerald-400" />
            </div>
            <div className="min-w-0">
              <h3 className="text-sm sm:text-base font-bold text-white tracking-tight truncate">
                Cargar Postulante & Evaluación con IA
              </h3>
              <p className="text-[11px] sm:text-xs text-slate-400 truncate">
                Ingesta de CV y diagnóstico psicométrico con Gemini AI
              </p>
            </div>
          </div>
          {!isProcessing && (
            <button
              onClick={onClose}
              className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer flex-shrink-0"
              title="Cerrar modal"
            >
              <X className="w-5 h-5" />
            </button>
          )}
        </div>

        {/* Content */}
        {isProcessing ? (
          <div className="p-8 sm:p-12 flex flex-col items-center justify-center text-center space-y-5 flex-1">
            <div className="relative">
              <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-full border-4 border-emerald-500/20 border-t-emerald-500 animate-spin flex items-center justify-center" />
              <Sparkles className="w-5 h-5 sm:w-6 sm:h-6 text-emerald-500 absolute inset-0 m-auto animate-pulse" />
            </div>
            <div className="space-y-1.5 max-w-md">
              <h4 className="text-base font-bold text-slate-900">
                Procesando Candidato con IA
              </h4>
              <p className="text-xs text-slate-600 font-medium animate-pulse">
                {processingStep}
              </p>
              <p className="text-[11px] text-slate-400 mt-2">
                Este proceso ejecuta el análisis de experiencia, validación de evidencia, interpretación DISC y redacción de la guía STAR en tiempo real.
              </p>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4 sm:space-y-5">
            {errorMsg && (
              <div className="p-3 bg-red-50 text-red-700 text-xs sm:text-sm rounded-lg border border-red-200 flex items-center space-x-2">
                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                <span>{errorMsg}</span>
              </div>
            )}

            {/* 1. Selección de Vacante */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center space-x-1.5">
                <Briefcase className="w-3.5 h-3.5 text-slate-500" />
                <span>Vacante o Convocatoria de Postulación *</span>
              </label>
              <select
                value={selectedPositionId}
                onChange={(e) => setSelectedPositionId(e.target.value)}
                className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 transition-all font-medium"
              >
                {positions.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.title} · {p.department} ({p.seniority})
                  </option>
                ))}
                <option value="OTHER">Otro cargo / Candidatura espontánea</option>
              </select>

              {selectedPositionId === 'OTHER' && (
                <input
                  type="text"
                  required
                  value={customRole}
                  onChange={(e) => setCustomRole(e.target.value)}
                  placeholder="Escriba el título del puesto objetivo..."
                  className="mt-2 w-full px-3.5 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              )}
            </div>

            {/* 2. Carga de Archivo PDF */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5 flex items-center space-x-1.5">
                <FileText className="w-3.5 h-3.5 text-slate-500" />
                <span>Archivo de Currículum Vítae (PDF) *</span>
              </label>

              <div
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
                className={`border-2 border-dashed rounded-xl p-5 text-center cursor-pointer transition-all ${
                  isDragging
                    ? 'border-emerald-500 bg-emerald-50/50'
                    : file
                    ? 'border-emerald-400 bg-emerald-50/20'
                    : 'border-slate-300 hover:border-slate-400 bg-slate-50/60'
                }`}
              >
                <input
                  type="file"
                  ref={fileInputRef}
                  accept=".pdf"
                  onChange={handleFileChange}
                  className="hidden"
                />

                {file ? (
                  <div className="flex items-center justify-center space-x-3 text-emerald-800">
                    <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0" />
                    <div className="text-left">
                      <p className="text-xs sm:text-sm font-bold truncate max-w-[280px] sm:max-w-md">
                        {file.name}
                      </p>
                      <p className="text-[11px] text-emerald-600">
                        {(file.size / (1024 * 1024)).toFixed(2)} MB · Clic o arrastra para cambiar
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="space-y-1">
                    <UploadCloud className="w-8 h-8 text-slate-400 mx-auto" />
                    <p className="text-xs sm:text-sm font-semibold text-slate-700">
                      Arrastra tu archivo PDF aquí o <span className="text-emerald-600 underline">explora tu equipo</span>
                    </p>
                    <p className="text-[11px] text-slate-400">
                      Formato PDF estándar hasta 15 MB con sanitización automática
                    </p>
                  </div>
                )}
              </div>
            </div>

            {/* 3. Datos Personales */}
            <div className="space-y-3">
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider flex items-center space-x-1.5">
                <User className="w-3.5 h-3.5 text-slate-500" />
                <span>Datos del Postulante</span>
              </label>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <input
                  type="text"
                  required
                  placeholder="Nombres *"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  className="px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
                <input
                  type="text"
                  required
                  placeholder="Apellidos *"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  className="px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <input
                  type="email"
                  required
                  placeholder="Correo Electrónico *"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
                <input
                  type="tel"
                  placeholder="Teléfono / Celular"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  className="px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-lg text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>
            </div>

            {/* 4. Parámetros DISC */}
            <div className="space-y-3 pt-1 border-t border-slate-200">
              <div className="flex items-center justify-between">
                <label className="text-xs font-semibold text-slate-700 uppercase tracking-wider flex items-center space-x-1.5">
                  <SlidersHorizontal className="w-3.5 h-3.5 text-slate-500" />
                  <span>Test Psicométrico DISC (Estilo: {getPrimaryStyle()})</span>
                </label>
                <button
                  type="button"
                  onClick={setAverageDisc}
                  className="text-xs text-slate-500 hover:text-slate-800 flex items-center space-x-1 cursor-pointer"
                >
                  <RotateCcw className="w-3 h-3" />
                  <span>50/50</span>
                </button>
              </div>

              <div className="grid grid-cols-1 xs:grid-cols-2 sm:grid-cols-4 gap-2.5 sm:gap-3 bg-slate-50 p-3 sm:p-3.5 rounded-xl border border-slate-200">
                <div>
                  <div className="flex justify-between text-xs font-bold text-slate-700 mb-1">
                    <span>D: Dominancia</span>
                    <span className="text-emerald-600">{dominance}%</span>
                  </div>
                  <input
                    type="range"
                    min={0}
                    max={100}
                    value={dominance}
                    onChange={(e) => setDominance(Number(e.target.value))}
                    className="w-full accent-emerald-500 cursor-pointer"
                  />
                </div>

                <div>
                  <div className="flex justify-between text-xs font-bold text-slate-700 mb-1">
                    <span>I: Influencia</span>
                    <span className="text-emerald-600">{influence}%</span>
                  </div>
                  <input
                    type="range"
                    min={0}
                    max={100}
                    value={influence}
                    onChange={(e) => setInfluence(Number(e.target.value))}
                    className="w-full accent-emerald-500 cursor-pointer"
                  />
                </div>

                <div>
                  <div className="flex justify-between text-xs font-bold text-slate-700 mb-1">
                    <span>S: Estabilidad</span>
                    <span className="text-emerald-600">{steadiness}%</span>
                  </div>
                  <input
                    type="range"
                    min={0}
                    max={100}
                    value={steadiness}
                    onChange={(e) => setSteadiness(Number(e.target.value))}
                    className="w-full accent-emerald-500 cursor-pointer"
                  />
                </div>

                <div>
                  <div className="flex justify-between text-xs font-bold text-slate-700 mb-1">
                    <span>C: Cumplimiento</span>
                    <span className="text-emerald-600">{conscientiousness}%</span>
                  </div>
                  <input
                    type="range"
                    min={0}
                    max={100}
                    value={conscientiousness}
                    onChange={(e) => setConscientiousness(Number(e.target.value))}
                    className="w-full accent-emerald-500 cursor-pointer"
                  />
                </div>
              </div>
            </div>
          </form>
        )}

        {/* Footer */}
        {!isProcessing && (
          <div className="px-4 sm:px-6 py-3 sm:py-4 bg-slate-50 border-t border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <span className="text-[11px] sm:text-xs text-slate-500 text-center sm:text-left">
              Evaluación asistida por Google Gemini AI
            </span>
            <div className="flex items-center space-x-2.5 w-full sm:w-auto">
              <button
                type="button"
                onClick={onClose}
                className="flex-1 sm:flex-none px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-700 text-xs sm:text-sm font-semibold rounded-lg transition-colors cursor-pointer text-center"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={handleSubmit}
                className="flex-1 sm:flex-none px-5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-xs sm:text-sm font-semibold rounded-lg shadow-sm transition-all flex items-center justify-center space-x-2 cursor-pointer"
              >
                <Sparkles className="w-4 h-4" />
                <span>Evaluar con IA</span>
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};