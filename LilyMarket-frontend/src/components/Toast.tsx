interface ToastProps {
  message: string
  type: 'success' | 'error'
}

export default function Toast({ message, type }: ToastProps) {
  const bgColor = type === 'success' ? 'bg-success' : 'bg-error'
  
  return (
    <div className={`${bgColor} text-white px-6 py-3 rounded-card shadow-lg animate-fadeIn`}>
      {message}
    </div>
  )
}