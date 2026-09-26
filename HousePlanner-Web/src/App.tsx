import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { Provider, useDispatch } from 'react-redux';
import store, { type AppDispatch } from './store';
import AppRoutes from './routes/AppRoutes';
import { verifySessionAsync } from './features/auth/authSlice';

const AuthenticatedRoutes: React.FC = () => {
 const dispatch = useDispatch<AppDispatch>();
 React.useEffect(() => { void dispatch(verifySessionAsync()); }, [dispatch]);
 return <AppRoutes />;
};

const App: React.FC = () => {
 return (
  <Provider store={store}>
   <BrowserRouter>
    <AuthenticatedRoutes />
   </BrowserRouter>
  </Provider>
 );
};

export default App;
