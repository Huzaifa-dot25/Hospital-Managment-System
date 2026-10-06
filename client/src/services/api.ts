import axios from 'axios';

const BASE_URL = 'http://localhost:5168/api/v1';

/**
 * Axios instance used throughout the entire app.
 *
 * Request interceptor  — attaches Bearer token from localStorage on every request.
 * Response interceptor — on 401, attempts a silent token refresh, then retries
 *                        the original request once. If refresh also fails, clears
 *                        storage and redirects to /login.
 */
const api = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// ──────────────────────────────────────────────────────────
// REQUEST: attach current access token
// ──────────────────────────────────────────────────────────
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// ──────────────────────────────────────────────────────────
// RESPONSE: handle 401 → refresh token once, then retry
// ──────────────────────────────────────────────────────────
let isRefreshing = false;
// Queue of requests that arrived while a refresh was in progress
let refreshSubscribers: ((token: string) => void)[] = [];

const subscribeTokenRefresh = (cb: (token: string) => void) => {
  refreshSubscribers.push(cb);
};

const onRefreshed = (newToken: string) => {
  refreshSubscribers.forEach((cb) => cb(newToken));
  refreshSubscribers = [];
};

const clearAuthAndRedirect = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  // Hard redirect so React Router re-initialises cleanly
  window.location.href = '/login';
};

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Only attempt refresh on 401 and only once per request (flag _retry)
    if (error.response?.status === 401 && !originalRequest._retry) {
      const refreshToken = localStorage.getItem('refreshToken');

      if (!refreshToken) {
        clearAuthAndRedirect();
        return Promise.reject(error);
      }

      if (isRefreshing) {
        // Another refresh is already in-flight — queue this request
        return new Promise((resolve) => {
          subscribeTokenRefresh((newToken) => {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            resolve(api(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const { data } = await axios.post(`${BASE_URL}/Auth/refresh-token`, {
          refreshToken,
        });

        const newToken: string = data?.data?.token ?? data?.token;
        const newRefreshToken: string =
          data?.data?.refreshToken ?? data?.refreshToken;

        localStorage.setItem('token', newToken);
        if (newRefreshToken) {
          localStorage.setItem('refreshToken', newRefreshToken);
        }

        api.defaults.headers.common.Authorization = `Bearer ${newToken}`;
        onRefreshed(newToken);

        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return api(originalRequest);
      } catch {
        clearAuthAndRedirect();
        return Promise.reject(error);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  },
);

export default api;
