import apiClient from '../services/apiClient';

export function getImageUrl(thumbnailUrl?: string | null): string {
  if (!thumbnailUrl) return '';
  if (thumbnailUrl.startsWith('http://') || thumbnailUrl.startsWith('https://')) return thumbnailUrl;
  
  const apiBase = apiClient.defaults.baseURL;
  if (!apiBase) {
    throw new Error('API Base URL is not configured. Missing VITE_API_BASE_URL.');
  }

  let host = '';
  try {
    const url = new URL(apiBase, window.location.origin);
    host = url.origin;
  } catch(e) {
    throw new Error(`Failed to parse API base URL: ${apiBase}`);
  }
  
  const path = thumbnailUrl.startsWith('/') ? thumbnailUrl : `/${thumbnailUrl}`;
  return `${host}${path}`;
}
