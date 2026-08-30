import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./hooks/useAuth";
import { RequireAuth } from "./components/RequireAuth";
import { Landing } from "./pages/Landing";
import { Login } from "./pages/Login";
import { SignUp } from "./pages/SignUp";
import { ClaimTypeSelector } from "./pages/ClaimTypeSelector";
import { ClaimDashboard } from "./pages/ClaimDashboard";
import { ClaimDetailPage } from "./pages/ClaimDetail";
import { ExecutorAccept } from "./pages/ExecutorAccept";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/login" element={<Login />} />
          <Route path="/signup" element={<SignUp />} />
          <Route path="/executor/accept" element={<RequireAuth><ExecutorAccept /></RequireAuth>} />
          <Route path="/claims" element={<RequireAuth><ClaimDashboard /></RequireAuth>} />
          <Route path="/claims/new" element={<RequireAuth><ClaimTypeSelector /></RequireAuth>} />
          <Route path="/claims/:claimId" element={<RequireAuth><ClaimDetailPage /></RequireAuth>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
