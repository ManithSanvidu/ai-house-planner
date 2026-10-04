import { describe, expect, it } from 'vitest';
import { formatDesignRef, formatProjectRef, formatRequestRef } from './referenceCode';

describe('reference code formatter', () => {
 it('formats a design GUID', () => {
  expect(formatDesignRef('b08e48d2-8df0-4c4b-8e5f-85704c3727c2')).toBe('DES-B08E48D2');
 });

 it('formats a project GUID', () => {
  expect(formatProjectRef('03c17193-8b95-4da6-a90e-4d29b322f8c6')).toBe('PRJ-03C17193');
 });

 it('formats a request GUID', () => {
  expect(formatRequestRef('915fb21c-7e4a-420d-8c31-6e9d684f2c01')).toBe('REQ-915FB21C');
 });

 it('removes hyphens, uppercases letters, and uses only the first eight characters', () => {
  expect(formatDesignRef('ab-cd-ef-12-34567890')).toBe('DES-ABCDEF12');
 });

 it.each([null, undefined, ''])('returns a safe fallback for %s', value => {
  expect(formatDesignRef(value)).toBe('DES-UNKNOWN');
 });
});
