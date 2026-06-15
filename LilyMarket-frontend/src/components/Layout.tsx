import { Outlet, Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/auth/login')
  }

  return (
    <div className="min-h-screen bg-primary">
      <header className="bg-card border-b border-accent/20 sticky top-0 z-40">
        <div className="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between">
          <Link to="/auctions" className="flex items-center gap-2">
            <span className="text-2xl">🌸</span>
            <h1 className="text-xl font-bold text-accent">Lily Market</h1>
          </Link>
          
          <nav className="flex items-center gap-4">
            {user ? (
              <>
                <Link 
                  to="/auctions/create" 
                  className="btn-primary text-sm py-2 px-4"
                >
                  + Создать аукцион
                </Link>
                <div className="flex items-center gap-3">
                  <span className="text-secondary text-sm hidden sm:inline">
                    {user.displayName}
                  </span>
                  <button 
                    onClick={handleLogout}
                    className="text-secondary hover:text-text transition-colors text-sm"
                  >
                    Выйти
                  </button>
                </div>
              </>
            ) : (
              <Link 
                to="/auth/login" 
                className="btn-primary text-sm py-2 px-4"
              >
                Войти
              </Link>
            )}
          </nav>
        </div>
      </header>
      
      <main className="max-w-7xl mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}