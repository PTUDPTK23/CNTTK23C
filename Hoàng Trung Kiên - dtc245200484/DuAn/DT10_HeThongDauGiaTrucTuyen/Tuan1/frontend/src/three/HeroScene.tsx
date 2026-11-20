import { useRef } from 'react'
import { Float, OrbitControls, ContactShadows, Environment, Lightformer, Sparkles } from '@react-three/drei'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

function Gavel() {
  const group = useRef<THREE.Group>(null)

  useFrame((state, delta) => {
    if (group.current) {
      group.current.rotation.y += delta * 0.22
      group.current.position.y = Math.sin(state.clock.elapsedTime * 0.7) * 0.06
    }
  })

  return (
    <group ref={group} rotation={[0.1, -0.5, 0.18]}>
      <mesh castShadow position={[0, 0.15, 0]}>
        <cylinderGeometry args={[0.1, 0.12, 2.4, 32]} />
        <meshStandardMaterial color="#7a5a1c" metalness={0.7} roughness={0.4} />
      </mesh>

      <mesh castShadow rotation={[0, 0, Math.PI / 2]} position={[0, 1.25, 0]}>
        <cylinderGeometry args={[0.38, 0.38, 1.05, 40]} />
        <meshStandardMaterial color="#d4af37" metalness={1} roughness={0.24} />
      </mesh>

      <mesh rotation={[0, 0, Math.PI / 2]} position={[0, 1.25, 0]}>
        <torusGeometry args={[0.39, 0.035, 12, 48]} />
        <meshStandardMaterial color="#f6e7b4" metalness={1} roughness={0.16} />
      </mesh>

      {[-0.3, 0.3].map((offset) => (
        <mesh key={offset} rotation={[0, 0, Math.PI / 2]} position={[offset, 1.25, 0]}>
          <torusGeometry args={[0.39, 0.018, 8, 36]} />
          <meshStandardMaterial color="#67e8f9" emissive="#22d3ee" emissiveIntensity={0.5} metalness={0.8} roughness={0.3} />
        </mesh>
      ))}

      <mesh position={[0, -1.15, 0]}>
        <cylinderGeometry args={[0.11, 0.15, 0.45, 24]} />
        <meshStandardMaterial color="#22d3ee" emissive="#06b6d4" emissiveIntensity={0.35} metalness={0.6} roughness={0.2} />
      </mesh>
    </group>
  )
}

function Halo() {
  const ring = useRef<THREE.Group>(null)

  useFrame((_state, delta) => {
    if (ring.current) ring.current.rotation.z += delta * 0.06
  })

  return (
    <group ref={ring} rotation={[1.25, 0.1, 0]}>
      <mesh>
        <torusGeometry args={[2.25, 0.012, 12, 160]} />
        <meshStandardMaterial color="#d4af37" metalness={1} roughness={0.3} transparent opacity={0.75} />
      </mesh>
      <mesh rotation={[0.4, 0.3, 0]}>
        <torusGeometry args={[2.6, 0.006, 8, 160]} />
        <meshStandardMaterial color="#22d3ee" emissive="#22d3ee" emissiveIntensity={0.6} metalness={0.5} roughness={0.4} />
      </mesh>
    </group>
  )
}

export default function HeroScene() {
  return (
    <>
      <ambientLight intensity={0.5} />
      <spotLight position={[5, 7, 5]} angle={0.4} penumbra={0.8} intensity={90} color="#fff4d6" castShadow />
      <spotLight position={[-6, 3, -4]} angle={0.6} penumbra={1} intensity={45} color="#22d3ee" />
      <pointLight position={[0, -2, 3]} intensity={12} color="#d4af37" />

      <Environment resolution={256}>
        <Lightformer form="rect" intensity={2.4} color="#f6e7b4" position={[0, 5, -6]} scale={[10, 5, 1]} />
        <Lightformer form="rect" intensity={1.5} color="#22d3ee" position={[-6, 1, 2]} scale={[4, 6, 1]} />
        <Lightformer form="circle" intensity={1.2} color="#ffffff" position={[5, 2, 4]} scale={[3, 3, 1]} />
      </Environment>

      <Float speed={1.2} rotationIntensity={0.18} floatIntensity={0.35}>
        <Gavel />
      </Float>
      <Halo />

      <Sparkles count={45} scale={7} size={1.8} speed={0.25} color="#efd794" opacity={0.55} />
      <ContactShadows position={[0, -2.1, 0]} opacity={0.55} scale={12} blur={2.6} far={5} color="#000000" />

      <OrbitControls
        enablePan={false}
        enableZoom={false}
        autoRotate
        autoRotateSpeed={0.55}
        minPolarAngle={Math.PI / 2.8}
        maxPolarAngle={Math.PI / 1.7}
      />
    </>
  )
}
