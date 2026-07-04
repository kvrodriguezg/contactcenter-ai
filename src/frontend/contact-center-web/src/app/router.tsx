import { Route, Routes } from 'react-router-dom';
import { MainLayout } from '../layouts/MainLayout';
import { HomePage } from '../features/home/HomePage';

export function AppRouter() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route path="/" element={<HomePage />} />
      </Route>
    </Routes>
  );
}
