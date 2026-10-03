import { afterEach, describe, expect, it } from 'vitest';
import apiClient from '../services/apiClient';
import { getImageUrl } from './imageUtils';

describe('getImageUrl', () => {
  const originalBaseUrl = apiClient.defaults.baseURL;

  afterEach(() => {
    apiClient.defaults.baseURL = originalBaseUrl;
  });

  it('returns an absolute Supabase URL unchanged', () => {
    const url = 'https://project.supabase.co/storage/v1/object/public/plan-library/plan/image.jpg';

    expect(getImageUrl(url)).toBe(url);
  });

  it('continues resolving legacy uploads through the API origin', () => {
    apiClient.defaults.baseURL = 'https://api.example.test/api/v1';

    expect(getImageUrl('/uploads/legacy.jpg')).toBe('https://api.example.test/uploads/legacy.jpg');
  });
});
