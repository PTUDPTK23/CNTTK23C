import { Component, Suspense, useState } from 'react'
import type { ErrorInfo, ReactNode } from 'react'
import { Canvas } from '@react-three/fiber'
import { ImageOff, Loader2 } from 'lucide-react'
import { mediaUrl } from '../lib/api'
import { supportsWebGL } from '../lib/webgl'

export function ImagePanel({
  imageUrl,
  alt,
  className = ''
}: {
  imageUrl?: string | null
  alt: string
  className?: string
}) {
  const [failed, setFailed] = useState(false)

  return (
    <div className={`relative grid h-full w-full place-items-center overflow-hidden bg-ink-900 ${className}`}>
      {imageUrl && !failed ? (
        <img
          src={mediaUrl(imageUrl)}
          alt={alt}
          className="h-full w-full object-cover"
          onError={() => setFailed(true)}
        />
      ) : (
        <div className="flex flex-col items-center gap-3 text-slate-600">
          <ImageOff size={30} />
          <span className="text-xs">Không thể hiển thị bản xem trước</span>
        </div>
      )}
    </div>
  )
}

class SceneErrorBoundary extends Component<
  { children: ReactNode; fallback: ReactNode; onError?: () => void },
  { failed: boolean }
> {
  state = { failed: false }

  static getDerivedStateFromError() {
    return { failed: true }
  }

  componentDidCatch(_error: Error, _info: ErrorInfo) {
    this.props.onError?.()
  }

  render() {
    if (this.state.failed) return this.props.fallback
    return this.props.children
  }
}

interface SceneShellProps {
  children: ReactNode
  imageUrl?: string | null
  alt: string
  className?: string
  cameraPosition?: [number, number, number]
  fov?: number
  allowZoom?: boolean
}

/**
 * Keeps WebGL optional: unsupported devices and runtime failures fall back
 * to a regular product image, so the auction stays fully usable.
 */
export function SceneShell({
  children,
  imageUrl,
  alt,
  className = '',
  cameraPosition = [0, 2.4, 6.4],
  fov = 40
}: SceneShellProps) {
  const [webgl] = useState(() => supportsWebGL())
  const [failed, setFailed] = useState(false)
  const fallback = <ImagePanel imageUrl={imageUrl} alt={alt} />

  if (!webgl || failed) {
    return <div className={`relative h-full w-full overflow-hidden ${className}`}>{fallback}</div>
  }

  return (
    <div className={`relative h-full w-full overflow-hidden bg-[radial-gradient(circle_at_50%_20%,rgba(34,211,238,0.08),transparent_55%),radial-gradient(circle_at_50%_70%,rgba(212,175,55,0.08),transparent_60%)] ${className}`}>
      <SceneErrorBoundary fallback={fallback} onError={() => setFailed(true)}>
        <Suspense
          fallback={
            <div className="grid h-full place-items-center">
              <Loader2 size={22} className="animate-spin text-gold-300/70" />
            </div>
          }
        >
          <Canvas
            dpr={[1, 1.6]}
            camera={{ position: cameraPosition, fov }}
            gl={{ antialias: true, alpha: true, powerPreference: 'high-performance' }}
          >
            {children}
          </Canvas>
        </Suspense>
      </SceneErrorBoundary>
    </div>
  )
}
