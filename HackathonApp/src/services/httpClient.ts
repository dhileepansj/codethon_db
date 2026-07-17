import axios from "axios";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "";

const httpClient = axios.create({
  baseURL: `${API_BASE}/hackathonapi`,
  headers: { "Content-Type": "application/json" },
});

// Attach JWT token
httpClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle 401
httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      sessionStorage.removeItem("token");
      sessionStorage.removeItem("user");
      window.location.href = `${import.meta.env.VITE_APP_BASEPATH || "/novaccodelab"}/login`;
    }
    return Promise.reject(error);
  }
);

export default httpClient;
