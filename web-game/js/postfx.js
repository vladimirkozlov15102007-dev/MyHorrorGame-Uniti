import * as THREE from 'three';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { ShaderPass } from 'three/addons/postprocessing/ShaderPass.js';
import { FXAAShader } from 'three/addons/shaders/FXAAShader.js';

export function createComposer(renderer, scene, camera) {
  const composer = new EffectComposer(renderer);
  composer.addPass(new RenderPass(scene, camera));

  const bloom = new UnrealBloomPass(
    new THREE.Vector2(window.innerWidth, window.innerHeight),
    0.8, 0.8, 0.55,
  );
  composer.addPass(bloom);

  // Chromatic aberration + vignette + film grain + subtle color grade
  const grade = new ShaderPass({
    uniforms: {
      tDiffuse: { value: null },
      uTime: { value: 0 },
      uCA: { value: 0.0018 },
      uVignette: { value: 1.0 },
      uGrain: { value: 0.06 },
      uHurt: { value: 0 },
    },
    vertexShader: `
      varying vec2 vUv;
      void main() { vUv = uv; gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0); }`,
    fragmentShader: `
      precision highp float;
      varying vec2 vUv;
      uniform sampler2D tDiffuse;
      uniform float uTime, uCA, uVignette, uGrain, uHurt;

      float hash(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

      void main(){
        vec2 uv = vUv;
        vec2 c = uv - 0.5;
        float r = texture2D(tDiffuse, uv + c * uCA).r;
        float g = texture2D(tDiffuse, uv).g;
        float b = texture2D(tDiffuse, uv - c * uCA).b;
        vec3 col = vec3(r,g,b);

        // vignette
        float v = 1.0 - length(c) * 1.15 * uVignette;
        col *= clamp(v, 0.0, 1.0);

        // film grain
        float n = hash(uv * 800.0 + uTime * 33.0);
        col += (n - 0.5) * uGrain;

        // subtle cold teal-orange grade
        col.r = mix(col.r, col.r * 1.05 + 0.02, 0.6);
        col.b = mix(col.b, col.b * 1.08 + 0.01, 0.6);
        col.g = col.g * 0.98;

        // hurt tint
        col.r += uHurt * 0.45;
        col.g -= uHurt * 0.1;
        col.b -= uHurt * 0.1;

        gl_FragColor = vec4(col, 1.0);
      }`,
  });
  composer.addPass(grade);

  const fxaa = new ShaderPass(FXAAShader);
  fxaa.material.uniforms['resolution'].value.set(1 / window.innerWidth, 1 / window.innerHeight);
  composer.addPass(fxaa);

  return { composer, bloom, grade, fxaa };
}

// ---- Dust particles in the air, drifting ----
export function createDustField(scene, count = 700, bounds = { x: 60, y: 8, z: 60, cx: 0, cy: 3, cz: -10 }) {
  const geo = new THREE.BufferGeometry();
  const pos = new Float32Array(count * 3);
  const vel = [];
  for (let i = 0; i < count; i++) {
    pos[i * 3] = (Math.random() - 0.5) * bounds.x + bounds.cx;
    pos[i * 3 + 1] = Math.random() * bounds.y + bounds.cy - bounds.y / 2;
    pos[i * 3 + 2] = (Math.random() - 0.5) * bounds.z + bounds.cz;
    vel.push({ x: (Math.random() - 0.5) * 0.05, y: Math.random() * 0.04 + 0.01, z: (Math.random() - 0.5) * 0.05 });
  }
  geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
  const mat = new THREE.PointsMaterial({
    color: 0xbfb39a, size: 0.05, transparent: true, opacity: 0.35,
    depthWrite: false, blending: THREE.AdditiveBlending,
  });
  const points = new THREE.Points(geo, mat);
  scene.add(points);
  return {
    points,
    update(dt, camera) {
      const p = geo.attributes.position.array;
      for (let i = 0; i < count; i++) {
        p[i * 3] += vel[i].x * dt;
        p[i * 3 + 1] += vel[i].y * dt;
        p[i * 3 + 2] += vel[i].z * dt;
        if (p[i * 3 + 1] > bounds.cy + bounds.y / 2) p[i * 3 + 1] = bounds.cy - bounds.y / 2;
      }
      geo.attributes.position.needsUpdate = true;
    },
  };
}

// ---- God rays: a few volumetric cone meshes from moon / windows ----
export function createGodRays(scene) {
  const rays = new THREE.Group();
  const mat = new THREE.MeshBasicMaterial({
    color: 0xaec3e0, transparent: true, opacity: 0.07, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide,
  });
  // a few shafts through the factory hall roof
  const positions = [
    { x: 2, z: -8, rot: 0.1 },
    { x: 10, z: 6, rot: -0.15 },
    { x: 16, z: -4, rot: 0.2 },
    { x: -2, z: 4, rot: -0.1 },
  ];
  for (const p of positions) {
    const cone = new THREE.Mesh(new THREE.ConeGeometry(2.4, 14, 10, 1, true), mat);
    cone.position.set(p.x, 5, p.z);
    cone.rotation.z = p.rot;
    rays.add(cone);
  }
  scene.add(rays);
  return rays;
}
