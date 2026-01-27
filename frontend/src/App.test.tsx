import { render } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import App from './App';
import { ThemeProvider } from './context/ThemeContext';

describe('App', () => {
    it('renders without crashing', () => {
        render(
            <ThemeProvider>
                <App />
            </ThemeProvider>
        );
        expect(document.body).toBeTruthy();
    });
});
