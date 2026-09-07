import Keycloak from 'keycloak-js';

// Keycloak client instance pointing to configured Keycloak server
export const keycloak = new Keycloak({
  url: (import.meta as any).env?.VITE_KEYCLOAK_URL || 'http://localhost:8085',
  realm: 'ats-realm',
  clientId: 'ats-frontend',
});

let initPromise: Promise<boolean> | null = null;

export const initKeycloak = (): Promise<boolean> => {
  if (initPromise) {
    return initPromise;
  }

  initPromise = new Promise(async (resolve, reject) => {
    try {
      const authenticated = await keycloak.init({
        onLoad: 'login-required',
        pkceMethod: 'S256',
        checkLoginIframe: false,
        enableLogging: true,
      });

      resolve(authenticated);
    } catch (error) {
      console.error('Error al inicializar Keycloak:', error);
      // Reset promise so subsequent retries can succeed
      initPromise = null;
      reject(error);
    }
  });

  return initPromise;
};
