import Keycloak from 'keycloak-js';

// Keycloak client instance pointing to configured Keycloak server
export const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8085',
  realm: 'ats-realm',
  clientId: 'ats-frontend',
});

let initPromise: Promise<boolean> | null = null;

export const initKeycloak = (): Promise<boolean> => {
  if (initPromise) {
    return initPromise;
  }

  initPromise = keycloak.init({
        onLoad: 'login-required',
        pkceMethod: 'S256',
        checkLoginIframe: false,
        enableLogging: import.meta.env.DEV,
  });

  return initPromise;
};
