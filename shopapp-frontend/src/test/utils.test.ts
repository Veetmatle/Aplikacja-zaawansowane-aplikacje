import { describe, it, expect } from 'vitest';
import { formatPrice, formatDate, cn } from '@/lib/utils';

describe('formatPrice', () => {
  it('formats PLN currency correctly', () => {
    const result = formatPrice(99.99);
    expect(result).toContain('99,99');
    expect(result).toMatch(/zł|PLN/);
  });

  it('formats zero', () => {
    const result = formatPrice(0);
    expect(result).toContain('0,00');
  });

  it('formats large numbers', () => {
    const result = formatPrice(12345.67);
    // Polish format uses space as thousands separator
    expect(result).toContain('12');
    expect(result).toContain('345,67');
  });
});

describe('formatDate', () => {
  it('formats ISO date to Polish locale', () => {
    const result = formatDate('2024-06-15T12:00:00Z');
    expect(result).toContain('2024');
    // Should contain Polish month name
    expect(result).toContain('czerw');
  });
});

describe('cn', () => {
  it('merges class names', () => {
    expect(cn('px-4', 'py-2')).toBe('px-4 py-2');
  });

  it('handles conditional classes', () => {
    expect(cn('base', false && 'hidden', 'visible')).toBe('base visible');
  });

  it('merges Tailwind conflicts', () => {
    expect(cn('px-4', 'px-6')).toBe('px-6');
  });
});
