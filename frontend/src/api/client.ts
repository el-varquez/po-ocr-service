const defaultBaseUrl = "http://localhost:5123";

export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, "") ?? defaultBaseUrl;
