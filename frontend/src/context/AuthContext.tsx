import React, { createContext, useContext, useState, useEffect } from 'react';
import { keycloak, initKeycloak } from '../services/keycloak';
import { UserProfile } from '../types';
import { AlertCircle, RefreshCw } from 'lucide-react';

interface AuthContextType {
  isAuthenticated: boolean;
  user: UserProfile | null;
  isAdmin: boolean;
  isRecruiter: boolean;
  token?: string;
  logout: () => void;
  login: () => void;
}

const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  user: null,
  isAdmin: false,
  isRecruiter: false,
  logout: () => {},
  login: () => {},
});

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [authError, setAuthError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    // Handle browser Back/Forward Cache (bfcache)
    // When the user hits the browser Back button after logout, reload to prevent stale JS state
    const handlePageShow = (event: PageTransitionEvent) => {
      if (event.persisted) {
        window.location.reload();
      }
    };
    window.addEventListener('pageshow', handlePageShow);

    // Setup Keycloak automatic token expiration & logout hooks
    keycloak.onTokenExpired = () => {
      keycloak.updateToken(30).catch(() => {
        console.warn('Token expirado y no se pudo renovar.');
        if (isMounted) {
          setIsAuthenticated(false);
          setUser(null);
        }
        keycloak.login();
      });
    };

    keycloak.onAuthLogout = () => {
      if (isMounted) {
        setIsAuthenticated(false);
        setUser(null);
      }
      keycloak.login();
    };

    const authenticate = async () => {
      try {
        setLoading(true);
        setAuthError(null);
        const authenticated = await initKeycloak();

        if (!isMounted) return;

        if (authenticated && keycloak.tokenParsed) {
          setIsAuthenticated(true);
          const parsedToken = keycloak.tokenParsed;
          const roles = parsedToken.realm_access?.roles ?? [];
          const hasAdmin = roles.includes('ats_admin');
          const hasRecruiter = roles.includes('ats_recruiter');

          if (!hasAdmin && !hasRecruiter) {
            setIsAuthenticated(false);
            setUser(null);
            setLoading(false);
            setAuthError('Acceso denegado: Su cuenta no cuenta con los roles requeridos (ats_admin o ats_recruiter) para ingresar al sistema.');
            return;
          }

          const role: 'ats_admin' | 'ats_recruiter' = hasAdmin
            ? 'ats_admin'
            : 'ats_recruiter';

          const userProfile: UserProfile = {
            id: parsedToken?.sub || '',
            username: parsedToken.preferred_username || '',
            fullName: parsedToken.name || parsedToken.preferred_username || 'Usuario ATS',
            email: parsedToken.email || '',
            role,
          };

          setUser(userProfile);
          setLoading(false);
        } else {
          setIsAuthenticated(false);
          setUser(null);
          setLoading(false);
          // Redirect to Keycloak login if not authenticated
          keycloak.login();
        }
      } catch (error) {
        console.error('Error al inicializar sesión en Keycloak:', error);
        if (isMounted) {
          setIsAuthenticated(false);
          setUser(null);
          setLoading(false);
          setAuthError(
            error instanceof Error ? error.message :
              'No se pudo conectar con el servidor de autenticación. Verifique que el servicio se encuentre activo.'
          );
        }
      }
    };

    authenticate();

    // Periodic token refresh every 60s
    const interval = setInterval(() => {
      if (keycloak.authenticated) {
        keycloak.updateToken(70).catch(() => {
          console.warn('Fallo al refrescar token Keycloak');
        });
      }
    }, 60000);

    return () => {
      isMounted = false;
      window.removeEventListener('pageshow', handlePageShow);
      clearInterval(interval);
    };
  }, []);

  const logout = () => {
    setIsAuthenticated(false);
    setUser(null);
    keycloak.logout({ redirectUri: window.location.origin });
  };

  const login = () => {
    setLoading(true);
    setAuthError(null);
    keycloak.login();
  };

  const isAdmin = user?.role === 'ats_admin';
  const isRecruiter = user?.role === 'ats_recruiter';

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-900 flex flex-col items-center justify-center space-y-4 p-4 text-center">
        <div className="w-12 h-12 border-4 border-slate-700 border-t-white rounded-full animate-spin"></div>
        <div className="text-white text-sm font-semibold tracking-wide">
          Verificando credenciales en Keycloak...
        </div>
        <div className="text-slate-400 text-xs">
          TalentIQ Enterprise ATS • Servidor de Identidad
        </div>
      </div>
    );
  }

  if (authError) {
    return (
      <div className="min-h-screen bg-slate-900 flex flex-col items-center justify-center p-4">
        <div className="max-w-md w-full bg-slate-800 border border-slate-700 rounded-2xl p-8 text-center space-y-5 shadow-2xl">
          <div className="w-14 h-14 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-400 flex items-center justify-center mx-auto">
            <AlertCircle className="w-7 h-7" />
          </div>
          <div className="space-y-2">
            <h2 className="text-base font-bold text-white">Error de Autenticación</h2>
            <p className="text-xs text-slate-300 leading-relaxed">
              {authError}
            </p>
          </div>
          <div className="pt-2 flex flex-col sm:flex-row gap-2">
            <button
              onClick={() => window.location.reload()}
              className="flex-1 py-2.5 bg-slate-700 hover:bg-slate-600 text-white text-xs font-semibold rounded-xl transition-all flex items-center justify-center space-x-1.5"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              <span>Recargar Página</span>
            </button>
            <button
              onClick={login}
              className="flex-1 py-2.5 bg-white hover:bg-slate-100 text-slate-900 text-xs font-bold rounded-xl transition-all shadow-sm"
            >
              <span>Iniciar Sesión</span>
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        isAdmin,
        isRecruiter,
        token: keycloak.token,
        logout,
        login,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
