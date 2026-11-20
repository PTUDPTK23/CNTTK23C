import { Suspense, lazy } from 'react'
import type { ReactNode } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AuthProvider } from './context/AuthContext'
import { ToastProvider } from './context/ToastContext'
import Home from './pages/Home'
import Auctions from './pages/Auctions'
import Login from './pages/Login'
import Register from './pages/Register'
import Profile from './pages/Profile'
import NotFound from './pages/NotFound'

const AuctionDetail = lazy(() => import('./pages/AuctionDetail'))
const AdminLayout = lazy(() => import('./pages/admin/AdminLayout'))
const DashboardPage = lazy(() => import('./pages/admin/Dashboard'))
const AdminProducts = lazy(() => import('./pages/admin/Products'))
const AdminAuctions = lazy(() => import('./pages/admin/Auctions'))
const AdminUsers = lazy(() => import('./pages/admin/Users'))
const AdminAudit = lazy(() => import('./pages/admin/Audit'))

function Lazy({ children }: { children: ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="container-lux grid min-h-[45vh] place-items-center">
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="h-2 w-2 animate-pulse rounded-full bg-gold-400" />
            Đang mở phòng đấu giá…
          </div>
        </div>
      }
    >
      {children}
    </Suspense>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <Routes>
            <Route element={<Layout />}>
              <Route index element={<Home />} />
              <Route path="auctions" element={<Auctions />} />
              <Route path="auctions/:id" element={<Lazy><AuctionDetail /></Lazy>} />
              <Route path="login" element={<Login />} />
              <Route path="register" element={<Register />} />
              <Route
                path="profile"
                element={
                  <ProtectedRoute>
                    <Profile />
                  </ProtectedRoute>
                }
              />
              <Route
                path="admin"
                element={
                  <ProtectedRoute requireAdmin>
                    <Lazy><AdminLayout /></Lazy>
                  </ProtectedRoute>
                }
              >
                <Route index element={<Lazy><DashboardPage /></Lazy>} />
                <Route path="products" element={<Lazy><AdminProducts /></Lazy>} />
                <Route path="auctions" element={<Lazy><AdminAuctions /></Lazy>} />
                <Route path="users" element={<Lazy><AdminUsers /></Lazy>} />
                <Route path="audit" element={<Lazy><AdminAudit /></Lazy>} />
              </Route>
              <Route path="*" element={<NotFound />} />
            </Route>
          </Routes>
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}
