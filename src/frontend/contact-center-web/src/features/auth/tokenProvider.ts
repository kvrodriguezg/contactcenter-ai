export interface TokenProvider {
  getToken(): Promise<string | null>;
  setToken?(token: string): void;
  clearToken(): void;
}

const TOKEN_KEY = 'contactcenterai_access_token';

export class LocalTokenProvider implements TokenProvider {
  async getToken(): Promise<string | null> {
    return localStorage.getItem(TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
  }
}

export class Auth0TokenProvider implements TokenProvider {
  private readonly getAccessTokenSilently: () => Promise<string>;

  constructor(getAccessTokenSilently: () => Promise<string>) {
    this.getAccessTokenSilently = getAccessTokenSilently;
  }

  async getToken(): Promise<string | null> {
    try {
      return await this.getAccessTokenSilently();
    } catch {
      return null;
    }
  }

  clearToken(): void {
    // Auth0 gestiona el token en su SDK; no se persiste en localStorage.
  }
}

let activeTokenProvider: TokenProvider = new LocalTokenProvider();

export function setTokenProvider(provider: TokenProvider): void {
  activeTokenProvider = provider;
}

export function getTokenProvider(): TokenProvider {
  return activeTokenProvider;
}
