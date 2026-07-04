import { CssBaseline, ThemeProvider } from '@mui/material';
import { BrowserRouter } from 'react-router-dom';
import { AppRouter } from './router';
import { appTheme } from './theme';

function App() {
  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <BrowserRouter>
        <AppRouter />
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
