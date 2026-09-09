import { useState } from 'react';
import {
  RefreshCw,
  Activity,
  Building2,
  LogOut,
  UploadCloud,
  Briefcase,
  Menu,
  X,
  ShieldCheck,
  UserCheck
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

interface NavbarProps {
  isBackendHealthy: boolean | null;
  onRefresh: () => void;
  isLoading: boolean;
  onOpenUploadModal?: () => void;
  onOpenPositionModal?: () => void;
}

export const Navbar = ({
  isBackendHealthy,
  onRefresh,
  isLoading,
  onOpenUploadModal,
  onOpenPositionModal,
}: NavbarProps) => {
  const { user, isAdmin, logout } = useAuth();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const handleMobileAction = (action?: () => void) => {
    setIsMobileMenuOpen(false);
    if (action) action();
  };

  return (
    <header className="sticky top-0 z-30 bg-slate-900 border-b border-slate-800 text-white shadow-sm">
      <div className="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        {/* Brand */}
        <div className="flex items-center space-x-2.5 sm:space-x-3.5 min-w-0">
          <div className="w-8 h-8 sm:w-9 sm:h-9 rounded-lg bg-slate-800 border border-slate-700 flex items-center justify-center text-slate-100 shadow-inner flex-shrink-0">
            <Building2 className="w-4 h-4 sm:w-5 sm:h-5 text-slate-300" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center space-x-2">
              <span className="font-bold text-base sm:text-lg tracking-tight text-white whitespace-nowrap">
                TalentIQ <span className="hidden xs:inline text-slate-400 font-normal text-xs uppercase tracking-widest ml-1">Enterprise ATS</span>
              </span>
              <span className="hidden sm:inline-block px-2.5 py-0.5 rounded text-[11px] font-semibold bg-slate-800 text-slate-300 border border-slate-700 uppercase tracking-wider">
                {isAdmin ? 'Administración' : 'Evaluador'}
              </span>
            </div>
            <p className="hidden md:block text-xs text-slate-400 font-normal truncate">
              Plataforma de Asistencia en Selección & Preparación de Entrevistas
            </p>
          </div>
        </div>

        {/* Desktop & Tablet Navigation (md and up) */}
        <div className="hidden md:flex items-center space-x-2.5 lg:space-x-3">
          {/* Health status */}
          <div className="hidden lg:flex items-center space-x-2 px-3 py-1.5 rounded-lg bg-slate-800/80 border border-slate-700/80 text-xs font-medium text-slate-300">
            <Activity className={`w-3.5 h-3.5 ${
              isBackendHealthy === true
                ? 'text-emerald-400'
                : isBackendHealthy === false
                ? 'text-red-400'
                : 'text-amber-400'
            }`} />
            <span className="text-xs">
              {isBackendHealthy === true
                ? 'Servicios Operativos'
                : isBackendHealthy === false
                ? 'Servicio No Disponible'
                : 'Conectando...'}
            </span>
          </div>

          {/* Refresh button */}
          <button
            onClick={onRefresh}
            disabled={isLoading}
            className="flex items-center space-x-1.5 px-3 py-1.5 text-xs sm:text-sm font-medium text-slate-200 hover:text-white bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-lg transition-colors disabled:opacity-50 shadow-sm cursor-pointer"
            title="Actualizar información de postulantes"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin text-slate-300' : ''}`} />
            <span className="hidden xl:inline">Actualizar</span>
          </button>

          {/* New Vacancy Button (Admin only) */}
          {isAdmin && onOpenPositionModal && (
            <button
              onClick={onOpenPositionModal}
              className="flex items-center space-x-1.5 px-3 py-1.5 text-xs sm:text-sm font-medium text-slate-200 hover:text-white bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-lg transition-colors shadow-sm cursor-pointer"
              title="Crear nueva vacante formal"
            >
              <Briefcase className="w-3.5 h-3.5 text-slate-300" />
              <span>Nueva Vacante</span>
            </button>
          )}

          {/* Upload Candidate Button (Admin & Recruiter) */}
          {onOpenUploadModal && (
            <button
              onClick={onOpenUploadModal}
              className="flex items-center space-x-1.5 px-3.5 py-1.5 text-xs sm:text-sm font-semibold text-white bg-emerald-600 hover:bg-emerald-500 rounded-lg transition-all shadow-sm cursor-pointer"
              title="Cargar currículum y evaluar con IA"
            >
              <UploadCloud className="w-4 h-4" />
              <span>Cargar Postulante</span>
            </button>
          )}

          {/* User Profile & Role Badge */}
          {user && (
            <div className="flex items-center space-x-3 pl-2 border-l border-slate-700">
              <div className="flex flex-col items-end text-right">
                <span className="text-sm font-bold text-slate-100 max-w-[140px] lg:max-w-[180px] truncate">
                  {user.fullName}
                </span>
                <span className={`text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded border ${
                  isAdmin
                    ? 'bg-purple-950/70 text-purple-300 border-purple-800'
                    : 'bg-emerald-950/70 text-emerald-300 border-emerald-800'
                }`}>
                  {isAdmin ? 'Administrador' : 'Reclutador'}
                </span>
              </div>

              {/* Logout Button */}
              <button
                onClick={logout}
                className="p-1.5 text-slate-400 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
                title="Cerrar Sesión (Keycloak)"
              >
                <LogOut className="w-4 h-4" />
              </button>
            </div>
          )}
        </div>

        {/* Mobile Navigation Controls (< md) */}
        <div className="flex md:hidden items-center space-x-2">
          {/* Quick Upload Button on Mobile */}
          {onOpenUploadModal && (
            <button
              onClick={onOpenUploadModal}
              className="flex items-center space-x-1 px-2.5 py-1.5 text-xs font-bold text-white bg-emerald-600 hover:bg-emerald-500 rounded-lg transition-all shadow-sm cursor-pointer"
              title="Cargar currículum"
            >
              <UploadCloud className="w-3.5 h-3.5" />
              <span className="text-[11px]">Cargar</span>
            </button>
          )}

          {/* Hamburger Menu Toggle */}
          <button
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            className="p-2 text-slate-300 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
            aria-label="Abrir menú de navegación"
          >
            {isMobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>
      </div>

      {/* Mobile Slide-down Menu Panel */}
      {isMobileMenuOpen && (
        <div className="md:hidden bg-slate-900 border-b border-slate-800 px-4 py-4 space-y-4 animate-in slide-in-from-top-2 duration-150">
          {/* User Profile Card */}
          {user && (
            <div className="p-3 bg-slate-800/80 border border-slate-700/80 rounded-xl flex items-center justify-between">
              <div className="flex items-center space-x-2.5">
                <div className="w-9 h-9 rounded-lg bg-slate-700 flex items-center justify-center text-slate-200">
                  {isAdmin ? <ShieldCheck className="w-5 h-5 text-purple-400" /> : <UserCheck className="w-5 h-5 text-emerald-400" />}
                </div>
                <div>
                  <p className="text-sm font-bold text-white truncate max-w-[180px]">{user.fullName}</p>
                  <p className="text-[11px] text-slate-400">{user.email || user.username}</p>
                </div>
              </div>
              <span className={`text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded border ${
                isAdmin
                  ? 'bg-purple-950/70 text-purple-300 border-purple-800'
                  : 'bg-emerald-950/70 text-emerald-300 border-emerald-800'
              }`}>
                {isAdmin ? 'Admin' : 'Reclutador'}
              </span>
            </div>
          )}

          {/* Health Status Indicator */}
          <div className="flex items-center space-x-2 px-3 py-2 rounded-lg bg-slate-800/60 border border-slate-700/60 text-xs font-medium text-slate-300">
            <Activity className={`w-3.5 h-3.5 ${
              isBackendHealthy === true
                ? 'text-emerald-400'
                : isBackendHealthy === false
                ? 'text-red-400'
                : 'text-amber-400'
            }`} />
            <span className="text-xs">
              {isBackendHealthy === true
                ? 'Servicios Backend Operativos'
                : isBackendHealthy === false
                ? 'Servicio No Disponible'
                : 'Conectando con Backend...'}
            </span>
          </div>

          {/* Mobile Actions List */}
          <div className="space-y-2">
            {/* New Vacancy Button (Admin only) */}
            {isAdmin && onOpenPositionModal && (
              <button
                onClick={() => handleMobileAction(onOpenPositionModal)}
                className="w-full flex items-center space-x-2.5 px-3.5 py-2.5 text-xs font-semibold text-slate-200 hover:text-white bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-xl transition-colors cursor-pointer"
              >
                <Briefcase className="w-4 h-4 text-slate-400" />
                <span>Registrar Nueva Vacante</span>
              </button>
            )}

            {/* Refresh Data */}
            <button
              onClick={() => handleMobileAction(onRefresh)}
              disabled={isLoading}
              className="w-full flex items-center space-x-2.5 px-3.5 py-2.5 text-xs font-semibold text-slate-200 hover:text-white bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-xl transition-colors disabled:opacity-50 cursor-pointer"
            >
              <RefreshCw className={`w-4 h-4 text-slate-400 ${isLoading ? 'animate-spin' : ''}`} />
              <span>Actualizar Postulantes</span>
            </button>

            {/* Logout */}
            <button
              onClick={() => handleMobileAction(logout)}
              className="w-full flex items-center space-x-2.5 px-3.5 py-2.5 text-xs font-semibold text-red-400 hover:text-red-300 bg-red-950/30 hover:bg-red-950/50 border border-red-900/40 rounded-xl transition-colors cursor-pointer"
            >
              <LogOut className="w-4 h-4 text-red-400" />
              <span>Cerrar Sesión</span>
            </button>
          </div>
        </div>
      )}
    </header>
  );
};

