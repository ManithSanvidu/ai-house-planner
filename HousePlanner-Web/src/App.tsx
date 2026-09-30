import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { Provider, useDispatch } from 'react-redux';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import store, { type AppDispatch } from './store';
import AppRoutes from './routes/AppRoutes';
import { verifySessionAsync } from './features/auth/authSlice';

const queryClient = new QueryClient({
 defaultOptions: { queries: { refetchOnWindowFocus: false, retry: false } },
});

const AuthenticatedRoutes: React.FC = () => {
 const dispatch = useDispatch<AppDispatch>();
 React.useEffect(() => { void dispatch(verifySessionAsync()); }, [dispatch]);
 return <AppRoutes />;
};

const App: React.FC = () => {
 return (
  <Provider store={store}>
   <QueryClientProvider client={queryClient}>
    <BrowserRouter>
     <AuthenticatedRoutes />
    </BrowserRouter>
   </QueryClientProvider>
  </Provider>
 );
};

export default App;
