import React, { useState } from 'react';
import {
  HelpCircle,
  ChevronDown,
  ChevronUp,
  UserCheck,
  FileSearch,
  CheckCircle2,
  Users,
  Compass,
  FileSignature,
  ArrowRight
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const QuickStartGuide: React.FC = () => {
  const { isAdmin, user } = useAuth();
  const [isOpen, setIsOpen] = useState<boolean>(false);

  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden transition-all">
      {/* Header Bar / Toggle Button */}
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="w-full px-4 sm:px-5 py-3 sm:py-3.5 bg-slate-50/80 hover:bg-slate-100/80 flex items-center justify-between text-left transition-colors border-b border-slate-100 cursor-pointer gap-2"
      >
        <div className="flex items-center space-x-2.5 min-w-0 flex-1">
          <div className="p-1.5 bg-slate-200 text-slate-700 rounded-lg flex-shrink-0">
            <HelpCircle className="w-4 h-4" />
          </div>
          <div className="min-w-0">
            <span className="text-xs font-bold text-slate-900 flex flex-wrap items-center gap-x-1">
              <span>Guía Rápida:</span>
              <span className="font-normal text-slate-600 truncate">
                {isAdmin
                  ? 'Supervisión y delegación de expedientes'
                  : 'Evaluación y dictamen de candidatos'}
              </span>
            </span>
            <p className="text-[11px] text-slate-500 truncate">
              {isOpen ? 'Haz clic para plegar esta guía' : 'Haz clic para desplegar los 3 pasos esenciales'}
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-600 flex-shrink-0">
          <span className="hidden sm:inline">{isOpen ? 'Ocultar guía' : 'Ver guía de 3 pasos'}</span>
          {isOpen ? <ChevronUp className="w-4 h-4 text-slate-500" /> : <ChevronDown className="w-4 h-4 text-slate-500" />}
        </div>
      </button>

      {/* Collapsible Content */}
      {isOpen && (
        <div className="p-4 sm:p-5 bg-white animate-in fade-in duration-200">
          {isAdmin ? (
            /* ADMIN 3-STEP FLOW */
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3 sm:gap-4">
              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="w-6 h-6 rounded-full bg-slate-900 text-white font-bold text-xs flex items-center justify-center">
                      1
                    </span>
                    <FileSearch className="w-4 h-4 text-slate-500" />
                  </div>
                  <h4 className="text-xs font-bold text-slate-900">Revisa las Postulaciones</h4>
                  <p className="text-[11px] text-slate-600 leading-relaxed">
                    La IA extrae información factual del CV, coteja requisitos técnicos declarados frente a la vacante y prepara el dossier con preguntas de apoyo para el entrevistador.
                  </p>
                </div>
                <div className="text-[10px] font-semibold text-slate-500 flex items-center space-x-1">
                  <span>Usa el buscador o filtros por cargo</span>
                  <ArrowRight className="w-3 h-3 text-slate-400" />
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="w-6 h-6 rounded-full bg-slate-900 text-white font-bold text-xs flex items-center justify-center">
                      2
                    </span>
                    <UserCheck className="w-4 h-4 text-blue-600" />
                  </div>
                  <h4 className="text-xs font-bold text-slate-900">Asigna un Evaluador</h4>
                  <p className="text-[11px] text-slate-600 leading-relaxed">
                    Haz clic en el botón <strong className="text-slate-800">"Asignar Evaluador"</strong> en cualquier candidato para delegarlo a un reclutador (ej. Carlos Mendoza o Laura Sánchez).
                  </p>
                </div>
                <div className="text-[10px] font-semibold text-blue-700 flex items-center space-x-1">
                  <span>Filtra por "Pendientes de Asignar"</span>
                  <ArrowRight className="w-3 h-3 text-blue-500" />
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="w-6 h-6 rounded-full bg-slate-900 text-white font-bold text-xs flex items-center justify-center">
                      3
                    </span>
                    <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                  </div>
                  <h4 className="text-xs font-bold text-slate-900">Supervisa las Resoluciones</h4>
                  <p className="text-[11px] text-slate-600 leading-relaxed">
                    Visualiza en tiempo real las decisiones oficiales (<strong className="text-emerald-700">Aprobado</strong>, <strong className="text-amber-700">En Reserva</strong> o <strong className="text-slate-700">No Seleccionado</strong>) y sus notas de entrevista.
                  </p>
                </div>
                <div className="text-[10px] font-semibold text-emerald-700 flex items-center space-x-1">
                  <span>Expediente auditado y descargable en PDF</span>
                </div>
              </div>
            </div>
          ) : (
            /* RECRUITER 3-STEP FLOW */
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="w-6 h-6 rounded-full bg-emerald-700 text-white font-bold text-xs flex items-center justify-center">
                      1
                    </span>
                    <Users className="w-4 h-4 text-emerald-600" />
                  </div>
                  <h4 className="text-xs font-bold text-slate-900">Tus Candidatos Asignados</h4>
                  <p className="text-[11px] text-slate-600 leading-relaxed">
                    Hola <strong>{user?.fullName || 'Evaluador'}</strong>. En tu panel aparecen los postulantes que la administración te ha delegado para entrevista técnica y evaluación de competencias.
                  </p>
                </div>
                <div className="text-[10px] font-semibold text-emerald-800 flex items-center space-x-1">
                  <span>Pestaña activa: "Mis Asignados"</span>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="w-6 h-6 rounded-full bg-emerald-700 text-white font-bold text-xs flex items-center justify-center">
                      2
                    </span>
                    <Compass className="w-4 h-4 text-slate-700" />
                  </div>
                  <h4 className="text-xs font-bold text-slate-900">Revisa el CV y Aplica la Guía STAR</h4>
                  <p className="text-[11px] text-slate-600 leading-relaxed">
                    Revisa el documento original pulsando <strong className="text-slate-800">"Ver CV"</strong> o abre el expediente para consultar la síntesis asistida, el perfil psicométrico y las preguntas situacionales STAR.
                  </p>
                </div>
                <div className="text-[10px] font-semibold text-slate-700 flex items-center space-x-1">
                  <span>Preguntas profesionales, técnicas y conductuales</span>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="w-6 h-6 rounded-full bg-emerald-700 text-white font-bold text-xs flex items-center justify-center">
                      3
                    </span>
                    <FileSignature className="w-4 h-4 text-slate-700" />
                  </div>
                  <h4 className="text-xs font-bold text-slate-900">Emite tu Dictamen Oficial</h4>
                  <p className="text-[11px] text-slate-600 leading-relaxed">
                    Al concluir la entrevista, selecciona tu resolución (<strong className="text-emerald-700">Aprobado</strong>, <strong className="text-amber-700">En Reserva</strong> o <strong className="text-slate-700">No Seleccionado</strong>) y registra tus notas confidenciales.
                  </p>
                </div>
                <div className="text-[10px] font-semibold text-slate-700 flex items-center space-x-1">
                  <span>Guardado seguro con un solo clic</span>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

