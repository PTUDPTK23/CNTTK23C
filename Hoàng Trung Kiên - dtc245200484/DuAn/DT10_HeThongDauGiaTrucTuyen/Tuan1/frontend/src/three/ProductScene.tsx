import { useRef } from 'react'
import { ContactShadows, Environment, Float, Lightformer, OrbitControls, Sparkles } from '@react-three/drei'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

const gold = { color: '#d4af37', metalness: 1, roughness: 0.22 }
const lightGold = { color: '#f6e7b4', metalness: 1, roughness: 0.15 }

function Watch() {
  return (
    <group rotation={[0.25, -0.35, 0]}>
      <mesh rotation={[0, 0, 0]}>
        <torusGeometry args={[0.85, 0.16, 20, 64]} />
        <meshStandardMaterial color="#101a2c" metalness={0.5} roughness={0.7} />
      </mesh>
      <group rotation={[Math.PI / 2, 0, 0]} position={[0, 0, 0.06]}>
        <mesh>
          <cylinderGeometry args={[0.62, 0.62, 0.22, 64]} />
          <meshStandardMaterial {...gold} />
        </mesh>
        <mesh position={[0, 0.12, 0]}>
          <cylinderGeometry args={[0.55, 0.55, 0.02, 64]} />
          <meshStandardMaterial color="#0a1524" metalness={0.6} roughness={0.35} />
        </mesh>
        <mesh position={[0, 0.135, 0]}>
          <boxGeometry args={[0.02, 0.34, 0.02]} />
          <meshStandardMaterial {...lightGold} />
        </mesh>
        <mesh position={[0.13, 0.14, 0]} rotation={[0, 0, -1.1]}>
          <boxGeometry args={[0.015, 0.24, 0.02]} />
          <meshStandardMaterial {...lightGold} />
        </mesh>
        {[0, 1, 2, 3].map((index) => (
          <mesh key={index} rotation={[0, (index * Math.PI) / 6, 0]} position={[0, 0.14, 0]}>
            <boxGeometry args={[0.01, 0.03, 0.01]} />
            <meshStandardMaterial color="#22d3ee" emissive="#22d3ee" emissiveIntensity={1.2} />
          </mesh>
        ))}
      </group>
      <mesh rotation={[0, 0, 0]} position={[0, 0, -0.02]}>
        <torusGeometry args={[0.64, 0.045, 12, 64]} />
        <meshStandardMaterial {...lightGold} />
      </mesh>
    </group>
  )
}

function Jewelry() {
  return (
    <group rotation={[0.15, 0, 0]}>
      <mesh position={[0, 0.35, 0]} scale={[0.85, 1.15, 0.85]}>
        <octahedronGeometry args={[0.72, 0]} />
        <meshPhysicalMaterial
          color="#34d399"
          metalness={0.15}
          roughness={0.05}
          clearcoat={1}
          clearcoatRoughness={0.05}
          envMapIntensity={2}
          emissive="#064e3b"
          emissiveIntensity={0.35}
        />
      </mesh>
      <mesh rotation={[Math.PI / 2, 0, 0]} position={[0, -0.45, 0]}>
        <torusGeometry args={[0.85, 0.055, 16, 72]} />
        <meshStandardMaterial {...gold} />
      </mesh>
      {Array.from({ length: 6 }).map((_, index) => {
        const angle = (index / 6) * Math.PI * 2
        return (
          <mesh
            key={index}
            position={[Math.cos(angle) * 0.52, -0.1, Math.sin(angle) * 0.52]}
            rotation={[0.4, angle, 0]}
          >
            <cylinderGeometry args={[0.025, 0.025, 0.55, 8]} />
            <meshStandardMaterial {...lightGold} />
          </mesh>
        )
      })}
    </group>
  )
}

function Car() {
  return (
    <group scale={0.78} rotation={[0, -0.5, 0]}>
      <mesh position={[0, 0.35, 0]}>
        <boxGeometry args={[3.1, 0.62, 1.35]} />
        <meshStandardMaterial color="#0d1728" metalness={0.95} roughness={0.18} />
      </mesh>
      <mesh position={[-0.15, 0.86, 0]}>
        <boxGeometry args={[1.55, 0.5, 1.18]} />
        <meshStandardMaterial color="#111f33" metalness={0.9} roughness={0.22} />
      </mesh>
      <mesh position={[-0.15, 0.9, 0.6]}>
        <boxGeometry args={[1.35, 0.32, 0.02]} />
        <meshStandardMaterial color="#67e8f9" metalness={0.4} roughness={0.1} emissive="#22d3ee" emissiveIntensity={0.25} />
      </mesh>
      {[1.02, -1.02].map((x) =>
        [0.68, -0.68].map((z) => (
          <mesh key={`${x}-${z}`} position={[x, 0.05, z]} rotation={[Math.PI / 2, 0, 0]}>
            <cylinderGeometry args={[0.38, 0.38, 0.24, 24]} />
            <meshStandardMaterial color="#05080f" metalness={0.4} roughness={0.8} />
          </mesh>
        ))
      )}
      <mesh position={[1.56, 0.35, 0]}>
        <boxGeometry args={[0.12, 0.3, 0.9]} />
        <meshStandardMaterial {...gold} />
      </mesh>
      <mesh position={[-1.56, 0.35, 0]}>
        <boxGeometry args={[0.12, 0.3, 0.9]} />
        <meshStandardMaterial {...gold} />
      </mesh>
    </group>
  )
}

function Sculpture() {
  return (
    <group>
      <mesh scale={0.95} rotation={[0.4, 0.6, 0]}>
        <torusKnotGeometry args={[0.82, 0.26, 220, 28]} />
        <meshStandardMaterial color="#c9a227" metalness={1} roughness={0.28} />
      </mesh>
      <mesh position={[0, -1.2, 0]}>
        <cylinderGeometry args={[0.55, 0.75, 0.28, 6]} />
        <meshStandardMaterial color="#0c1421" metalness={0.5} roughness={0.6} />
      </mesh>
    </group>
  )
}

function Manuscript() {
  return (
    <group rotation={[0.05, 0.3, 0]}>
      <mesh>
        <boxGeometry args={[1.5, 2.05, 0.38]} />
        <meshStandardMaterial color="#3b2418" metalness={0.2} roughness={0.55} />
      </mesh>
      <mesh position={[0, 0, 0.2]}>
        <boxGeometry args={[1.38, 1.92, 0.04]} />
        <meshStandardMaterial color="#e8dcc0" roughness={0.9} metalness={0.05} />
      </mesh>
      {[0.65, -0.65].map((y) => (
        <mesh key={y} position={[0, y, 0.235]}>
          <boxGeometry args={[1.3, 0.055, 0.01]} />
          <meshStandardMaterial {...gold} />
        </mesh>
      ))}
      <mesh position={[0, 0.6, 0.245]}>
        <boxGeometry args={[0.55, 0.4, 0.005]} />
        <meshStandardMaterial color="#d4af37" metalness={0.9} roughness={0.35} emissive="#7a5a1c" emissiveIntensity={0.2} />
      </mesh>
    </group>
  )
}

function Synthesizer() {
  return (
    <group rotation={[0.12, -0.25, 0]} scale={1.05}>
      <mesh>
        <boxGeometry args={[2.9, 0.55, 1.35]} />
        <meshStandardMaterial color="#2a1b12" metalness={0.3} roughness={0.4} />
      </mesh>
      <mesh position={[0, 0.16, 0.45]}>
        <boxGeometry args={[2.5, 0.08, 0.42]} />
        <meshStandardMaterial color="#e8dcc0" roughness={0.7} />
      </mesh>
      {Array.from({ length: 12 }).map((_, index) => (
        <mesh key={index} position={[-1.05 + index * 0.19, 0.21, 0.32]}>
          <boxGeometry args={[0.13, 0.02, 0.18]} />
          <meshStandardMaterial color="#ffffff" roughness={0.6} />
        </mesh>
      ))}
      {[0.25, 0.55].map((x) =>
        [0.15, 0.5, 0.85].map((z) => (
          <mesh key={`${x}-${z}`} position={[x, 0.32, -0.45 + z * 0.35]} rotation={[Math.PI / 2, 0, 0]}>
            <cylinderGeometry args={[0.1, 0.1, 0.12, 20]} />
            <meshStandardMaterial {...gold} />
          </mesh>
        ))
      )}
      <mesh position={[-0.85, 0.3, -0.55]}>
        <boxGeometry args={[0.55, 0.09, 0.3]} />
        <meshStandardMaterial color="#22d3ee" emissive="#22d3ee" emissiveIntensity={0.45} metalness={0.5} roughness={0.3} />
      </mesh>
    </group>
  )
}

function Chair() {
  return (
    <group rotation={[0.1, -0.5, 0]} scale={1.1}>
      <mesh position={[0, 0, 0]}>
        <boxGeometry args={[1.25, 0.22, 1.15]} />
        <meshStandardMaterial color="#6b3f22" roughness={0.55} metalness={0.1} />
      </mesh>
      <mesh position={[0, 0.82, -0.48]} rotation={[-0.25, 0, 0]}>
        <boxGeometry args={[1.15, 1.5, 0.18]} />
        <meshStandardMaterial color="#7a4a28" roughness={0.5} metalness={0.08} />
      </mesh>
      {[0.48, -0.48].map((x) =>
        [0.42, -0.42].map((z) => (
          <mesh key={`${x}-${z}`} position={[x, -0.6, z]} rotation={[0.06, 0, 0]}>
            <cylinderGeometry args={[0.07, 0.05, 1.15, 12]} />
            <meshStandardMaterial color="#3a2416" roughness={0.4} metalness={0.1} />
          </mesh>
        ))
      )}
      <mesh position={[1.0, 0.12, 0]}>
        <boxGeometry args={[0.7, 0.12, 1.0]} />
        <meshStandardMaterial color="#7a4a28" roughness={0.5} />
      </mesh>
    </group>
  )
}

function Katana() {
  return (
    <group rotation={[0, 0, 0.55]} scale={0.92}>
      <mesh position={[0, 0.9, 0]}>
        <boxGeometry args={[0.09, 2.5, 0.035]} />
        <meshStandardMaterial color="#cbd5e1" metalness={1} roughness={0.12} />
      </mesh>
      <mesh position={[0, -0.5, 0]}>
        <torusGeometry args={[0.22, 0.05, 12, 32]} />
        <meshStandardMaterial {...gold} />
      </mesh>
      <mesh position={[0, -1.35, 0]}>
        <cylinderGeometry args={[0.085, 0.085, 1.45, 20]} />
        <meshStandardMaterial color="#0b0f16" metalness={0.4} roughness={0.75} />
      </mesh>
      {[0.4, 0.05, -0.3].map((y) => (
        <mesh key={y} position={[0, -1.2 + y, 0]}>
          <torusGeometry args={[0.09, 0.012, 8, 24]} />
          <meshStandardMaterial {...gold} />
        </mesh>
      ))}
    </group>
  )
}

function Meteorite() {
  return (
    <group>
      <mesh rotation={[0.4, 0.8, 0.2]} scale={[1.15, 0.95, 1.05]}>
        <dodecahedronGeometry args={[1.05, 1]} />
        <meshStandardMaterial color="#4a5568" metalness={0.65} roughness={0.85} flatShading />
      </mesh>
      {[[0.65, 0.4, 0.5], [-0.5, -0.25, 0.6], [0.2, -0.6, -0.3]].map(([x, y, z], index) => (
        <mesh key={index} position={[x, y, z]} scale={0.22}>
          <dodecahedronGeometry args={[1, 0]} />
          <meshStandardMaterial color="#2d3748" metalness={0.5} roughness={0.9} flatShading />
        </mesh>
      ))}
      <mesh rotation={[Math.PI / 2, 0, 0]} position={[0, -1.25, 0]}>
        <torusGeometry args={[0.85, 0.04, 12, 64]} />
        <meshStandardMaterial color="#22d3ee" emissive="#22d3ee" emissiveIntensity={0.7} metalness={0.6} roughness={0.3} />
      </mesh>
    </group>
  )
}

function Artifact({ category }: { category: string }) {
  const value = category.toLowerCase()
  if (value.includes('watch') || value.includes('đồng hồ')) return <Watch />
  if (value.includes('jewel') || value.includes('trang sức')) return <Jewelry />
  if (value.includes('vehicle') || value.includes('xe')) return <Car />
  if (value.includes('art') || value.includes('nghệ thuật')) return <Sculpture />
  if (value.includes('book') || value.includes('sách')) return <Manuscript />
  if (value.includes('music') || value.includes('nhạc')) return <Synthesizer />
  if (value.includes('furniture') || value.includes('nội thất')) return <Chair />
  if (value.includes('collect') || value.includes('sưu tầm')) return <Katana />
  return <Meteorite />
}

export default function ProductScene({ category }: { category: string }) {
  const group = useRef<THREE.Group>(null)

  useFrame((_state, delta) => {
    if (group.current) {
      group.current.rotation.y += delta * 0.16
    }
  })

  return (
    <>
      <ambientLight intensity={0.55} />
      <spotLight position={[4, 6, 5]} angle={0.45} penumbra={0.9} intensity={90} color="#fff6dd" castShadow />
      <spotLight position={[-5, 2, -3]} angle={0.7} penumbra={1} intensity={40} color="#22d3ee" />
      <pointLight position={[0, -1, 4]} intensity={10} color="#d4af37" />

      <Environment resolution={256}>
        <Lightformer form="rect" intensity={2.2} color="#f6e7b4" position={[0, 5, -5]} scale={[8, 5, 1]} />
        <Lightformer form="rect" intensity={1.4} color="#22d3ee" position={[-5, 1, 3]} scale={[4, 5, 1]} />
        <Lightformer form="rect" intensity={1.1} color="#ffffff" position={[5, 1, 3]} scale={[4, 5, 1]} />
      </Environment>

      <Float speed={1} rotationIntensity={0.12} floatIntensity={0.3}>
        <group ref={group} position={[0, 0.35, 0]}>
          <Artifact category={category} />
        </group>
      </Float>

      <group position={[0, -1.85, 0]}>
        <mesh receiveShadow>
          <cylinderGeometry args={[1.75, 1.95, 0.3, 72]} />
          <meshStandardMaterial color="#080e1a" metalness={0.55} roughness={0.45} />
        </mesh>
        <mesh position={[0, 0.16, 0]}>
          <torusGeometry args={[1.78, 0.015, 8, 96]} />
          <meshStandardMaterial color="#d4af37" metalness={1} roughness={0.25} />
        </mesh>
        <mesh position={[0, 0.17, 0]} rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[1.35, 1.55, 72]} />
          <meshBasicMaterial color="#22d3ee" transparent opacity={0.09} side={THREE.DoubleSide} />
        </mesh>
      </group>

      <Sparkles count={28} scale={6} size={1.4} speed={0.2} color="#efd794" opacity={0.4} />
      <ContactShadows position={[0, -1.7, 0]} opacity={0.6} scale={10} blur={2.8} far={5.5} color="#000000" />

      <OrbitControls
        makeDefault
        enablePan={false}
        enableZoom
        minDistance={4.4}
        maxDistance={9}
        minPolarAngle={Math.PI / 4}
        maxPolarAngle={Math.PI / 1.85}
        autoRotate
        autoRotateSpeed={0.4}
      />
    </>
  )
}
