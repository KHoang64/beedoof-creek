import * as THREE from "three/webgpu";
import { FBXLoader } from "three/addons/loaders/FBXLoader.js";
import * as SkeletonUtils from "three/addons/utils/SkeletonUtils.js";
const fbxManager = new THREE.LoadingManager();
// Source FBXs name Unity textures; the browser assigns its own compressed materials after parsing.
const transparentPixel =
  "data:image/gif;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=";
fbxManager.setURLModifier((url) =>
  /\.(png|jpe?g)$/i.test(url.split("?")[0]) ? transparentPixel : url,
);
const $ = (id) => document.getElementById(id);
const host = $("scene"),
  rainCanvas = $("rain"),
  rainCtx = rainCanvas.getContext("2d"),
  music = $("music");
const touch = matchMedia("(pointer: coarse)").matches,
  clamp = THREE.MathUtils.clamp;
let mode = "menu",
  paused = false,
  filmTime = 0,
  rainOn = true,
  locked = false,
  yaw = -0.24,
  pitch = 0.075,
  pixelRatio = 1;
let joy = { x: 0, y: 0, id: null },
  lookId = null,
  lookX = 0,
  lookY = 0,
  fpsTime = 0,
  fpsFrames = 0;
const keys = new Set(),
  ducks = [],
  ripples = [];
const random = (() => {
  let n = 1957;
  return () => {
    n ^= n << 13;
    n ^= n >>> 17;
    n ^= n << 5;
    return (n >>> 0) / 4294967296;
  };
})();
const rng = (a, b) => a + (b - a) * random(),
  smooth = (t) => {
    t = clamp(t, 0, 1);
    return t * t * (3 - 2 * t);
  };
const radius = (x, z) => Math.hypot(x / 34, (z - 18) / 29);
const ground = (x, z) => {
  const r =
      radius(x, z) +
      Math.sin(x * 0.43 + z * 0.27) * Math.sin(z * 0.39 - x * 0.18) * 0.024,
    b = smooth((r - 0.82) / 0.215);
  return (
    -1.7 +
    2.13 * b +
    Math.max(0, r - 1.05) * 2.3 +
    Math.max(0, x - 24) * 0.09 * b +
    Math.sin(x * 0.77 + z * 0.52) * 0.1 * b
  );
};
const scene = new THREE.Scene();
scene.fog = new THREE.FogExp2(0x9eaea4, 0.0055);
const camera = new THREE.PerspectiveCamera(60, 1, 0.08, 310);
let renderer;
try {
  renderer = new THREE.WebGPURenderer({
    antialias: false,
    powerPreference: "high-performance",
  });
  await renderer.init();
} catch (e) {
  console.warn("WebGPU unavailable; using WebGL 2", e);
  renderer = new THREE.WebGPURenderer({ antialias: false, forceWebGL: true });
  await renderer.init();
}
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.18;
host.appendChild(renderer.domElement);
renderer.domElement.tabIndex = 0;
function resize() {
  const w = Math.max(1, innerWidth),
    h = Math.max(1, innerHeight);
  camera.aspect = w / h;
  camera.fov = h > w ? 64 : 59;
  camera.updateProjectionMatrix();
  pixelRatio = Math.min(
    devicePixelRatio || 1,
    touch ? 1.3 : 1.6,
    Math.sqrt((touch ? 880000 : 1800000) / (w * h)),
    pixelRatio || 1.3,
  );
  renderer.setPixelRatio(pixelRatio);
  renderer.setSize(w, h);
  const d = Math.min(devicePixelRatio || 1, 1.5);
  rainCanvas.width = Math.round(w * d);
  rainCanvas.height = Math.round(h * d);
}
addEventListener("resize", resize);
resize();
scene.add(new THREE.HemisphereLight(0xdce5de, 0x526b50, 1.5));
const sun = new THREE.DirectionalLight(0xe8e8d8, 0.8);
sun.position.set(-20, 55, -25);
scene.add(sun);
const fill = new THREE.DirectionalLight(0x9ebac1, 0.22);
fill.position.set(30, 12, 45);
scene.add(fill);
function hash(x, y, z) {
  let n = (x * 374761393 + y * 668265263 + z * 1442695041) | 0;
  n = Math.imul(n ^ (n >>> 13), 1274126177);
  return ((n ^ (n >>> 16)) >>> 0) / 4294967295;
}
function noise(x, y, z) {
  let ix = Math.floor(x),
    iy = Math.floor(y),
    iz = Math.floor(z),
    fx = smooth(x - ix),
    fy = smooth(y - iy),
    fz = smooth(z - iz),
    lerp = (a, b, t) => a + (b - a) * t;
  return lerp(
    lerp(
      lerp(hash(ix, iy, iz), hash(ix + 1, iy, iz), fx),
      lerp(hash(ix, iy + 1, iz), hash(ix + 1, iy + 1, iz), fx),
      fy,
    ),
    lerp(
      lerp(hash(ix, iy, iz + 1), hash(ix + 1, iy, iz + 1), fx),
      lerp(hash(ix, iy + 1, iz + 1), hash(ix + 1, iy + 1, iz + 1), fx),
      fy,
    ),
    fz,
  );
}
function makeSky() {
  const c = document.createElement("canvas");
  c.width = 512;
  c.height = 256;
  const ctx = c.getContext("2d"),
    im = ctx.createImageData(512, 256);
  for (let y = 0; y < 256; y++)
    for (let x = 0; x < 512; x++) {
      const lon = (x / 512) * Math.PI * 2,
        lat = (y / 255 - 0.5) * Math.PI,
        dx = Math.cos(lat) * Math.cos(lon),
        dy = Math.sin(lat),
        dz = Math.cos(lat) * Math.sin(lon);
      let n = 0,
        a = 0.5,
        f = 5.5;
      for (let k = 0; k < 4; k++) {
        n += noise(dx * f + 3.4, dy * f + 1.7, dz * f + 2.1) * a;
        f *= 2.07;
        a *= 0.53;
      }
      const t = clamp(
          smooth((n - 0.3) / 0.4) + 0.06 * Math.pow(1 - Math.max(0, dy), 6),
          0,
          1,
        ),
        i = (y * 512 + x) * 4;
      im.data[i] = 106 + 120 * t;
      im.data[i + 1] = 117 + 114 * t;
      im.data[i + 2] = 125 + 104 * t;
      im.data[i + 3] = 255;
    }
  ctx.putImageData(im, 0, 0);
  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.wrapS = THREE.RepeatWrapping;
  const sky = new THREE.Mesh(
    new THREE.SphereGeometry(290, 32, 16),
    new THREE.MeshBasicMaterial({
      map: tex,
      side: THREE.BackSide,
      fog: false,
      depthWrite: false,
    }),
  );
  sky.frustumCulled = false;
  scene.add(sky);
  return sky;
}
const sky = makeSky();
const loader = new THREE.TextureLoader();
function texture(name) {
  const t = loader.load("/assets/" + name);
  t.colorSpace = THREE.SRGBColorSpace;
  t.anisotropy = 2;
  return t;
}
const duckMap = texture("duck-brown.webp"),
  featherMap = texture("duck-feather.webp");
function terrain() {
  const g = new THREE.PlaneGeometry(180, 180, 150, 150);
  g.rotateX(-Math.PI / 2);
  const p = g.attributes.position,
    col = new Float32Array(p.count * 3),
    grass = new THREE.Color(0x3c693e),
    moss = new THREE.Color(0x50794a),
    mud = new THREE.Color(0x42392c),
    wet = new THREE.Color(0x3f5844),
    c = new THREE.Color();
  for (let i = 0; i < p.count; i++) {
    const x = p.getX(i),
      z = p.getZ(i) + 25,
      r = radius(x, z),
      pathX = 14 - 0.27 * (z + 20),
      path =
        Math.exp(-Math.pow((x - pathX) / 2.4, 2)) *
        (z < 18 ? 1 : smooth((28 - z) / 10)),
      rim = smooth((r - 0.97) / 0.16) * (1 - smooth((r - 1.35) / 0.35)),
      v =
        Math.sin(x * 0.47 + z * 0.21) * 0.08 +
        Math.sin(z * 0.72 - x * 0.31) * 0.055;
    c.copy(grass)
      .lerp(moss, clamp(0.3 + v + rim * 0.45, 0, 1))
      .lerp(mud, clamp(path * 0.92 + rim * 0.15, 0, 1));
    if (r < 1.02) c.lerp(wet, 0.45);
    p.setY(i, ground(x, z));
    p.setZ(i, z);
    col[i * 3] = c.r;
    col[i * 3 + 1] = c.g;
    col[i * 3 + 2] = c.b;
  }
  g.setAttribute("color", new THREE.BufferAttribute(col, 3));
  g.computeVertexNormals();
  scene.add(
    new THREE.Mesh(
      g,
      new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.92 }),
    ),
  );
  const leaves = new THREE.InstancedMesh(
      new THREE.PlaneGeometry(0.3, 0.15),
      new THREE.MeshBasicMaterial({ color: 0x8d7955, side: THREE.DoubleSide }),
      180,
    ),
    d = new THREE.Object3D();
  for (let i = 0; i < 180; i++) {
    const z = rng(-29, 10),
      x = 14 - 0.27 * (z + 20) + rng(-2.4, 2.4);
    d.position.set(x, ground(x, z) + 0.04, z);
    d.rotation.set(-Math.PI / 2, 0, rng(0, 6.28));
    d.scale.set(rng(0.5, 1.4), rng(0.5, 1.4), 1);
    d.updateMatrix();
    leaves.setMatrixAt(i, d.matrix);
  }
  leaves.instanceMatrix.needsUpdate = true;
  scene.add(leaves);
}
terrain();
const waterGeo = new THREE.CircleGeometry(1, 96);
waterGeo.rotateX(-Math.PI / 2);
const water = new THREE.Mesh(
  waterGeo,
  new THREE.MeshPhysicalMaterial({
    color: 0x18352f,
    roughness: 0.27,
    metalness: 0.27,
    transparent: true,
    opacity: 0.94,
    side: THREE.DoubleSide,
    depthWrite: false,
  }),
);
water.scale.set(34, 1, 29);
water.position.set(0, 0.014, 18);
water.renderOrder = 1;
scene.add(water);
function waterMoss() {
  const blobs = [];
  for (let z = -10; z < 50; z += 5.2)
    for (let x = -36; x < 38; x += 5.4) {
      const px = x + rng(-2, 2),
        pz = z + rng(-2, 2);
      if (radius(px, pz) <= 1.03 && (px * px) / 361 + (pz + 3) ** 2 / 100 >= 1)
        blobs.push([px, pz, rng(1.6, 4), rng(1.4, 3.7)]);
    }
  const geo = new THREE.CircleGeometry(1, 7);
  geo.rotateX(-Math.PI / 2);
  const m = new THREE.InstancedMesh(
      geo,
      new THREE.MeshBasicMaterial({
        color: 0x9bbd65,
        depthWrite: false,
        side: THREE.DoubleSide,
      }),
      blobs.length,
    ),
    d = new THREE.Object3D();
  blobs.forEach(([x, z, sx, sz], i) => {
    d.position.set(x, 0.05 + i * 0.00008, z);
    d.scale.set(sx, 1, sz);
    d.rotation.y = rng(0, 6);
    d.updateMatrix();
    m.setMatrixAt(i, d.matrix);
  });
  m.instanceMatrix.needsUpdate = true;
  m.renderOrder = 2;
  scene.add(m);
  const islands = new THREE.InstancedMesh(
    new THREE.CircleGeometry(1, 6),
    new THREE.MeshBasicMaterial({
      color: 0xa7c975,
      transparent: true,
      opacity: 0.6,
      depthWrite: false,
      side: THREE.DoubleSide,
    }),
    125,
  );
  for (let i = 0; i < 125; i++) {
    const a = rng(0, 6.28),
      r = rng(16, 22);
    d.position.set(
      Math.cos(a) * r,
      0.07 + i * 0.00002,
      -3 + Math.sin(a) * r * 0.55,
    );
    d.rotation.set(-Math.PI / 2, 0, rng(0, 6));
    d.scale.set(rng(0.3, 2), rng(0.2, 0.9), 1);
    d.updateMatrix();
    islands.setMatrixAt(i, d.matrix);
  }
  islands.instanceMatrix.needsUpdate = true;
  islands.renderOrder = 3;
  scene.add(islands);
}
waterMoss();
const trunkMat = new THREE.MeshStandardMaterial({
    color: 0x514a3c,
    roughness: 0.96,
  }),
  pineMat = new THREE.MeshStandardMaterial({
    color: 0x315d47,
    roughness: 1,
    side: THREE.DoubleSide,
  }),
  broadMat = new THREE.MeshStandardMaterial({ color: 0x326a40, roughness: 1 }),
  shrubMat = new THREE.MeshStandardMaterial({ color: 0x3b7545, roughness: 1 }),
  dummy = new THREE.Object3D();
function pines() {
  const count = 140,
    stems = new THREE.InstancedMesh(
      new THREE.CylinderGeometry(0.15, 0.28, 1, 5),
      trunkMat,
      count,
    ),
    leaves = new THREE.InstancedMesh(
      new THREE.ConeGeometry(1, 1, 7),
      pineMat,
      count * 4,
    ),
    tint = new THREE.Color();
  for (let row = 0, i = 0; row < 5; row++)
    for (let j = 0; j < 28; j++, i++) {
      const x = -78 + j * 5.8 + rng(-2, 2),
        z = 47 + row * 9 + rng(-3, 3),
        h = rng(13, 22),
        y = ground(x, z),
        r = h * rng(0.18, 0.26);
      dummy.position.set(x, y + h * 0.45, z);
      dummy.scale.set(1, h * 0.9, 1);
      dummy.rotation.set(0, 0, 0);
      dummy.updateMatrix();
      stems.setMatrixAt(i, dummy.matrix);
      for (let layer = 0; layer < 4; layer++) {
        dummy.position.set(x, y + h * (0.38 + layer * 0.17), z);
        dummy.scale.set(
          r * (1 - layer * 0.18),
          h * (0.35 - layer * 0.04),
          r * (1 - layer * 0.18),
        );
        dummy.rotation.y = rng(0, 6.28);
        dummy.updateMatrix();
        leaves.setMatrixAt(i * 4 + layer, dummy.matrix);
        tint.setHSL(rng(0.3, 0.4), rng(0.22, 0.4), rng(0.22, 0.38));
        leaves.setColorAt(i * 4 + layer, tint);
      }
    }
  stems.instanceMatrix.needsUpdate = leaves.instanceMatrix.needsUpdate = true;
  leaves.instanceColor.needsUpdate = true;
  scene.add(stems, leaves);
}
pines();
function broadleaf() {
  const spots = [];
  for (let i = 0; i < 42; i++) {
    const a = rng(0, 6.283),
      x = Math.cos(a) * rng(38, 54),
      z = 18 + Math.sin(a) * rng(34, 45);
    if (z < -12 && x > -22 && x < 25) continue;
    spots.push([x, z, rng(8, 14)]);
  }
  spots.push(
    [25, -12, 11],
    [-25, -11, 13],
    [29, 5, 14],
    [-10, -17, 17],
    [2, -30, 12],
    [22, -9, 11],
  );
  const stems = new THREE.InstancedMesh(
      new THREE.CylinderGeometry(0.22, 0.48, 1, 7),
      trunkMat,
      spots.length,
    ),
    crowns = new THREE.InstancedMesh(
      new THREE.IcosahedronGeometry(1, 1),
      broadMat,
      spots.length * 5,
    ),
    tint = new THREE.Color();
  spots.forEach(([x, z, h], i) => {
    const y = ground(x, z);
    dummy.position.set(x, y + h * 0.38, z);
    dummy.scale.set(1, h * 0.76, 1);
    dummy.rotation.set(0, 0, 0);
    dummy.updateMatrix();
    stems.setMatrixAt(i, dummy.matrix);
    for (let b = 0; b < 5; b++) {
      const a = b * 2.39996,
        r = b === 0 ? 0 : h * 0.16;
      dummy.position.set(
        x + Math.cos(a) * r,
        y + h * (0.78 + (b % 2) * 0.09),
        z + Math.sin(a) * r,
      );
      dummy.scale.set(
        h * rng(0.23, 0.32),
        h * rng(0.18, 0.27),
        h * rng(0.23, 0.32),
      );
      dummy.rotation.y = rng(0, 6);
      dummy.updateMatrix();
      crowns.setMatrixAt(i * 5 + b, dummy.matrix);
      tint.setHSL(rng(0.27, 0.35), rng(0.28, 0.48), rng(0.23, 0.42));
      crowns.setColorAt(i * 5 + b, tint);
    }
  });
  stems.instanceMatrix.needsUpdate = crowns.instanceMatrix.needsUpdate = true;
  crowns.instanceColor.needsUpdate = true;
  scene.add(stems, crowns);
  const root = new THREE.Vector3(-10, ground(-10, -17), -17),
    wood = new THREE.MeshStandardMaterial({ color: 0x2b2925, roughness: 1 });
  branch(root, root.clone().add(new THREE.Vector3(-1, 8, 0)), 1, wood);
  branch(
    root.clone().add(new THREE.Vector3(-1, 8, 0)),
    root.clone().add(new THREE.Vector3(-6, 11, 1)),
    0.42,
    wood,
  );
  branch(
    root.clone().add(new THREE.Vector3(-1, 8, 0)),
    root.clone().add(new THREE.Vector3(4, 13, 2)),
    0.46,
    wood,
  );
}
function branch(a, b, r, material) {
  const v = new THREE.Vector3().subVectors(b, a),
    m = new THREE.Mesh(
      new THREE.CylinderGeometry(r * 0.75, r, v.length(), 8),
      material,
    );
  m.position.copy(a).add(b).multiplyScalar(0.5);
  m.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), v.normalize());
  scene.add(m);
  return m;
}
broadleaf();
function understory() {
  const count = touch ? 2200 : 3400,
    grass = new THREE.InstancedMesh(
      new THREE.ConeGeometry(0.12, 1, 3),
      new THREE.MeshStandardMaterial({
        color: 0x4c8742,
        roughness: 1,
        side: THREE.DoubleSide,
      }),
      count,
    ),
    shrubs = new THREE.InstancedMesh(
      new THREE.IcosahedronGeometry(1, 1),
      shrubMat,
      370,
    ),
    ferns = new THREE.InstancedMesh(
      new THREE.ConeGeometry(1, 1, 5),
      new THREE.MeshStandardMaterial({ color: 0x548649, roughness: 1 }),
      400,
    ),
    tint = new THREE.Color();
  let n = 0,
    s = 0,
    f = 0,
    attempt = 0;
  while (n < count && attempt++ < 25000) {
    const x = rng(-57, 57),
      z = rng(-33, 69),
      r = radius(x, z);
    if (
      r < 1.015 ||
      r > 1.62 ||
      (z < 18 && Math.abs(x - (14 - 0.27 * (z + 20))) < 2.1)
    )
      continue;
    const y = ground(x, z),
      h = rng(0.28, 1.2);
    dummy.position.set(x, y + h * 0.43, z);
    dummy.scale.set(rng(0.6, 1.4), h, rng(0.5, 1.25));
    dummy.rotation.set(0, rng(0, 6.28), 0);
    dummy.updateMatrix();
    grass.setMatrixAt(n, dummy.matrix);
    tint.setHSL(rng(0.26, 0.39), rng(0.36, 0.59), rng(0.21, 0.41));
    grass.setColorAt(n++, tint);
    if (s < shrubs.count && random() < 0.12) {
      const k = rng(0.35, 1.05);
      dummy.position.set(x, y + k * 0.5, z);
      dummy.scale.set(k, k * 0.65, k);
      dummy.updateMatrix();
      shrubs.setMatrixAt(s, dummy.matrix);
      tint.setHSL(rng(0.28, 0.38), rng(0.27, 0.48), rng(0.2, 0.39));
      shrubs.setColorAt(s++, tint);
    }
    if (f < ferns.count && random() < 0.14) {
      const k = rng(0.45, 1.4);
      dummy.position.set(x, y + k * 0.3, z);
      dummy.scale.set(k, k * 0.65, k);
      dummy.updateMatrix();
      ferns.setMatrixAt(f, dummy.matrix);
      tint.setHSL(rng(0.29, 0.38), rng(0.3, 0.5), rng(0.25, 0.44));
      ferns.setColorAt(f++, tint);
    }
  }
  grass.count = n;
  shrubs.count = s;
  ferns.count = f;
  for (const m of [grass, shrubs, ferns]) {
    m.instanceMatrix.needsUpdate = true;
    m.instanceColor.needsUpdate = true;
    scene.add(m);
  }
}
understory();
const logMat = new THREE.MeshStandardMaterial({
  color: 0x3c392a,
  roughness: 0.94,
});
branch(
  new THREE.Vector3(-9, 0.04, -3),
  new THREE.Vector3(5, 0.18, -5),
  0.25,
  logMat,
);
branch(
  new THREE.Vector3(1, 0.13, -4.4),
  new THREE.Vector3(4, 0.8, -2.5),
  0.09,
  logMat,
);
branch(
  new THREE.Vector3(20, 0.16, 1),
  new THREE.Vector3(29, 0.6, -8),
  0.22,
  logMat,
);
function sign() {
  const x = 6,
    z = -16.5,
    y = ground(x, z),
    wood = new THREE.MeshStandardMaterial({ color: 0x483729, roughness: 1 }),
    post = new THREE.Mesh(new THREE.BoxGeometry(0.19, 1.75, 0.2), wood),
    board = new THREE.Mesh(new THREE.BoxGeometry(2.5, 0.72, 0.12), wood);
  post.position.set(x, y + 0.82, z);
  board.position.set(x, y + 1.55, z - 0.04);
  board.rotation.y = -0.18;
  scene.add(post, board);
  const c = document.createElement("canvas");
  c.width = 512;
  c.height = 160;
  const ctx = c.getContext("2d");
  ctx.fillStyle = "#eedb9e";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.font = "bold 58px Georgia";
  ctx.fillText("BeeDoof Creek", 256, 81);
  const label = new THREE.Mesh(
    new THREE.PlaneGeometry(2.42, 0.65),
    new THREE.MeshBasicMaterial({
      map: new THREE.CanvasTexture(c),
      transparent: true,
      depthWrite: false,
    }),
  );
  label.position.set(x, y + 1.55, z - 0.11);
  label.rotation.y = Math.PI - 0.18;
  scene.add(label);
}
sign();
function puddles() {
  const mat = new THREE.MeshBasicMaterial({
    color: 0x5c7775,
    transparent: true,
    opacity: 0.63,
    depthWrite: false,
    side: THREE.DoubleSide,
  });
  for (const [x, z, sx, sz] of [
    [13.7, -25.3, 2.2, 0.8],
    [17.8, -16.5, 1.6, 0.6],
    [15.6, -8, 1.4, 0.5],
  ]) {
    const m = new THREE.Mesh(new THREE.CircleGeometry(1, 32), mat);
    m.rotation.x = -Math.PI / 2;
    m.scale.set(sx, sz, 1);
    m.position.set(x, ground(x, z) + 0.05, z);
    scene.add(m);
  }
}
puddles();
function duckFallback() {
  const g = new THREE.Group(),
    brown = new THREE.MeshStandardMaterial({ color: 0xab8064 }),
    cream = new THREE.MeshStandardMaterial({ color: 0xddc4a4 }),
    orange = new THREE.MeshStandardMaterial({ color: 0xc78132 }),
    black = new THREE.MeshStandardMaterial({ color: 0x302820 });
  function part(mat, x, y, z, sx, sy, sz) {
    const m = new THREE.Mesh(new THREE.SphereGeometry(1, 10, 7), mat);
    m.position.set(x, y, z);
    m.scale.set(sx, sy, sz);
    g.add(m);
  }
  part(brown, 0, 0.37, 0, 0.52, 0.33, 0.32);
  part(cream, 0.3, 0.67, 0, 0.22, 0.23, 0.22);
  part(orange, 0.54, 0.59, 0, 0.24, 0.075, 0.12);
  part(black, 0.42, 0.73, -0.17, 0.05, 0.05, 0.05);
  part(black, 0.42, 0.73, 0.17, 0.05, 0.05, 0.05);
  return g;
}
for (let i = 0; i < 7; i++) {
  const swim = i >= 5,
    x = swim ? (i === 5 ? -5 : 4) : -6.8 + i * 2.25,
    z = swim ? (i === 5 ? 1 : -0.4) : -3 - (x + 9) / 7,
    g = duckFallback();
  g.position.set(x, swim ? -0.03 : 0.31 + ((x + 9) / 14) * 0.14, z);
  g.rotation.y = swim ? 0 : rng(2.25, 4.2);
  g.scale.setScalar(1.1);
  scene.add(g);
  ducks.push({ g, swim, phase: i * 1.93, base: g.position.y });
}
async function actualDucks() {
  try {
    const original = await new FBXLoader(fbxManager).loadAsync(
      "/assets/duck.fbx",
    );
    original.traverse((child) => {
      if (!child.isMesh) return;
      const name = Array.isArray(child.material)
          ? child.material.map((m) => m.name).join(" ")
          : child.material?.name || "",
        map = /feather/i.test(name) ? featherMap : duckMap,
        mat = new THREE.MeshStandardMaterial({
          map,
          color: 0xd8c1a5,
          roughness: 0.9,
          side: THREE.DoubleSide,
        });
      child.material = Array.isArray(child.material)
        ? child.material.map(() => mat)
        : mat;
    });
    const box = new THREE.Box3().setFromObject(original),
      size = box.getSize(new THREE.Vector3());
    if (!Number.isFinite(size.y) || size.y < 0.01)
      throw Error("Invalid duck bounds");
    for (const duck of ducks) {
      const model = SkeletonUtils.clone(original),
        scale = 1.15 / size.y;
      model.scale.setScalar(scale);
      model.position.set(
        -box.getCenter(new THREE.Vector3()).x * scale,
        -box.min.y * scale,
        -box.getCenter(new THREE.Vector3()).z * scale,
      );
      duck.g.clear();
      duck.g.add(model);
    }
    console.info("Original Patchmesh duck mesh loaded");
  } catch (e) {
    console.warn("Using duck fallback", e);
  }
}
async function actualPines() {
  const fbx = new FBXLoader(fbxManager);
  for (let type = 1; type <= 3; type++) {
    try {
      const original = await fbx.loadAsync(`/assets/pine-${type}.fbx`);
      original.traverse((child) => {
        if (!child.isMesh) return;
        if (/LOD[12]/i.test(child.name)) {
          child.visible = false;
          return;
        }
        const name = Array.isArray(child.material)
            ? child.material.map((m) => m.name).join(" ")
            : child.material?.name || "",
          mat = /bark|trunk|stem/i.test(name) ? trunkMat : pineMat;
        child.material = Array.isArray(child.material)
          ? child.material.map(() => mat)
          : mat;
      });
      const box = new THREE.Box3().setFromObject(original),
        size = box.getSize(new THREE.Vector3());
      if (!size.y || !Number.isFinite(size.y)) continue;
      for (let i = 0; i < 4; i++) {
        const x = -25 + (type - 1) * 21 + i * 5.8 + rng(-1, 1),
          z = 45 + rng(-2, 3),
          m = original.clone(true);
        m.scale.setScalar(rng(15, 21) / size.y);
        m.position.set(x, ground(x, z) - box.min.y * m.scale.x, z);
        m.rotation.y = rng(0, 6.28);
        scene.add(m);
      }
    } catch (e) {
      console.warn("Pine model unavailable", type, e);
    }
  }
}
function pondRipples() {
  for (let i = 0; i < 13; i++) {
    const mat = new THREE.MeshBasicMaterial({
        color: 0xb0c7ad,
        transparent: true,
        opacity: 0.26,
        side: THREE.DoubleSide,
        depthWrite: false,
      }),
      m = new THREE.Mesh(new THREE.RingGeometry(0.97, 1, 24), mat),
      a = rng(0, 6.28),
      r = rng(0, 22);
    m.rotation.x = -Math.PI / 2;
    m.position.set(Math.cos(a) * r, 0.09, 18 + Math.sin(a) * r * 0.8);
    m.renderOrder = 4;
    scene.add(m);
    ripples.push({ m, phase: rng(0, 1) });
  }
}
pondRipples();
function cameraDirection() {
  camera.rotation.order = "YXZ";
  camera.rotation.y = yaw;
  camera.rotation.x = pitch;
}
function reset() {
  camera.position.set(14, ground(14, -23) + 1.62, -23);
  yaw = Math.PI - (innerHeight > innerWidth ? 0.75 : 0.65);
  pitch = innerHeight > innerWidth ? -0.18 : -0.07;
  cameraDirection();
}
reset();
function menu() {
  mode = "menu";
  $("menu").hidden = false;
  $("hud").hidden = true;
  document.exitPointerLock?.();
}
function start(next) {
  mode = next;
  paused = false;
  filmTime = 0;
  $("menu").hidden = true;
  $("hud").hidden = false;
  $("film-controls").hidden = next !== "film";
  $("touch-controls").hidden = next !== "explore";
  $("desktop-hint").hidden = next !== "explore";
  if (next === "explore") reset();
  music.play().catch(() => {});
}
$("explore").onclick = () => start("explore");
$("watch").onclick = () => start("film");
$("menu-button").onclick = menu;
$("reset").onclick = () => {
  if (mode === "film") filmTime = 0;
  else reset();
};
$("pause").onclick = () => {
  paused = !paused;
  $("pause").textContent = paused ? "Play" : "Pause";
};
$("replay").onclick = () => {
  filmTime = 0;
  paused = false;
  $("pause").textContent = "Pause";
};
$("rain-toggle").onclick = () => {
  rainOn = !rainOn;
  $("rain-toggle").textContent = rainOn ? "Rain on" : "Rain off";
  $("rain-toggle").setAttribute("aria-pressed", String(rainOn));
  if (!rainOn) rainCtx.clearRect(0, 0, rainCanvas.width, rainCanvas.height);
};
let volume = 0.32;
try {
  volume = clamp(Number(localStorage.getItem("creek-volume") ?? 0.32), 0, 1);
} catch {}
$("volume").value = volume;
function setVolume(v) {
  music.volume = v;
  $("volume-value").textContent = Math.round(v * 100) + "%";
  try {
    localStorage.setItem("creek-volume", String(v));
  } catch {}
}
$("volume").oninput = (e) => setVolume(Number(e.target.value));
setVolume(volume);
renderer.domElement.addEventListener("click", () => {
  if (mode === "explore" && !touch) renderer.domElement.requestPointerLock?.();
});
document.addEventListener(
  "pointerlockchange",
  () => (locked = document.pointerLockElement === renderer.domElement),
);
addEventListener("keydown", (e) => {
  if (
    [
      "KeyW",
      "KeyA",
      "KeyS",
      "KeyD",
      "ArrowUp",
      "ArrowDown",
      "ArrowLeft",
      "ArrowRight",
      "Space",
    ].includes(e.code)
  )
    e.preventDefault();
  keys.add(e.code);
  if (e.code === "KeyR") {
    if (mode === "film") filmTime = 0;
    else reset();
  }
  if (e.code === "Escape" && mode !== "menu") menu();
  if (e.code === "Space" && mode === "film") {
    paused = !paused;
    $("pause").textContent = paused ? "Play" : "Pause";
  }
});
addEventListener("keyup", (e) => keys.delete(e.code));
addEventListener("blur", () => {
  keys.clear();
  joy.x = joy.y = 0;
});
addEventListener("mousemove", (e) => {
  if (mode !== "explore" || !locked) return;
  yaw -= e.movementX * 0.0022;
  pitch = clamp(pitch - e.movementY * 0.0022, -1.2, 1.2);
});
const joyEl = $("joystick"),
  stick = $("stick"),
  look = $("look-pad");
function setJoy(e) {
  const r = joyEl.getBoundingClientRect(),
    x = e.clientX - r.left - r.width / 2,
    y = e.clientY - r.top - r.height / 2,
    limit = r.width * 0.32,
    len = Math.max(1, Math.hypot(x, y));
  joy.x = clamp(x / limit, -1, 1);
  joy.y = clamp(y / limit, -1, 1);
  if (len > limit) {
    joy.x = x / len;
    joy.y = y / len;
  }
  stick.style.transform = `translate(${joy.x * limit}px,${joy.y * limit}px)`;
}
joyEl.addEventListener("pointerdown", (e) => {
  joyEl.setPointerCapture(e.pointerId);
  joy.id = e.pointerId;
  setJoy(e);
});
joyEl.addEventListener("pointermove", (e) => {
  if (joy.id === e.pointerId) setJoy(e);
});
function clearJoy(e) {
  if (joy.id !== e.pointerId) return;
  joy.id = null;
  joy.x = joy.y = 0;
  stick.style.transform = "";
}
joyEl.addEventListener("pointerup", clearJoy);
joyEl.addEventListener("pointercancel", clearJoy);
look.addEventListener("pointerdown", (e) => {
  look.setPointerCapture(e.pointerId);
  lookId = e.pointerId;
  lookX = e.clientX;
  lookY = e.clientY;
});
look.addEventListener("pointermove", (e) => {
  if (lookId !== e.pointerId) return;
  yaw -= (e.clientX - lookX) * 0.004;
  pitch = clamp(pitch - (e.clientY - lookY) * 0.004, -1.2, 1.2);
  lookX = e.clientX;
  lookY = e.clientY;
});
look.addEventListener("pointerup", (e) => {
  if (lookId === e.pointerId) lookId = null;
});
look.addEventListener("pointercancel", (e) => {
  if (lookId === e.pointerId) lookId = null;
});
const shots = [
    {
      a: [14, 3.1, -23],
      b: [12, 2.8, -21],
      ta: [4, 1, 15],
      tb: [2, 1, 15],
      d: 12,
    },
    {
      a: [-1, 1.15, -12],
      b: [1, 1.05, -11],
      ta: [-2, 0.45, -3.7],
      tb: [-1, 0.45, -3.8],
      d: 16,
    },
    {
      a: [17, 2.1, -10],
      b: [13, 2.2, -8],
      ta: [-5, 3, 29],
      tb: [-14, 4, 34],
      d: 13,
    },
    {
      a: [9, 2.8, -18],
      b: [14, 3.1, -23],
      ta: [0, 1.8, 16],
      tb: [0, 2, 15],
      d: 11,
    },
  ],
  filmLength = 52;
function film(t) {
  t %= filmLength;
  for (const s of shots) {
    if (t > s.d) {
      t -= s.d;
      continue;
    }
    const u = smooth(t / s.d);
    camera.position.fromArray(s.a).lerp(new THREE.Vector3().fromArray(s.b), u);
    camera.lookAt(
      new THREE.Vector3()
        .fromArray(s.ta)
        .lerp(new THREE.Vector3().fromArray(s.tb), u),
    );
    return;
  }
}
function movement(dt) {
  let forward =
      (keys.has("KeyW") || keys.has("ArrowUp") ? 1 : 0) -
      (keys.has("KeyS") || keys.has("ArrowDown") ? 1 : 0) -
      joy.y,
    side =
      (keys.has("KeyD") || keys.has("ArrowRight") ? 1 : 0) -
      (keys.has("KeyA") || keys.has("ArrowLeft") ? 1 : 0) +
      joy.x,
    len = Math.hypot(forward, side);
  if (len > 1) {
    forward /= len;
    side /= len;
  }
  const speed = keys.has("ShiftLeft") ? 7.5 : 4.2,
    dx = (-Math.sin(yaw) * forward + Math.cos(yaw) * side) * speed * dt,
    dz = (-Math.cos(yaw) * forward - Math.sin(yaw) * side) * speed * dt,
    x = clamp(camera.position.x + dx, -84, 84),
    z = clamp(camera.position.z + dz, -58, 106);
  if (radius(x, z) >= 1.02) {
    camera.position.x = x;
    camera.position.z = z;
  }
  camera.position.y = ground(camera.position.x, camera.position.z) + 1.62;
  cameraDirection();
}
const drops = Array.from({ length: touch ? 48 : 88 }, () => ({
  x: random(),
  y: random(),
  v: rng(0.25, 0.55),
  l: rng(7, 18),
}));
function rain(dt) {
  if (!rainOn || document.hidden) return;
  const w = rainCanvas.width,
    h = rainCanvas.height;
  rainCtx.clearRect(0, 0, w, h);
  rainCtx.strokeStyle = "rgba(222,237,231,.23)";
  rainCtx.lineWidth = Math.max(1, (w / innerWidth) * 0.7);
  rainCtx.beginPath();
  for (const p of drops) {
    p.y += p.v * dt;
    p.x -= 0.06 * dt;
    if (p.y > 1.05) {
      p.y = -0.05;
      p.x = random();
    }
    if (p.x < -0.05) p.x = 1.05;
    rainCtx.moveTo(p.x * w, p.y * h);
    rainCtx.lineTo(p.x * w - p.l * 0.35, p.y * h + p.l);
  }
  rainCtx.stroke();
}
let last = performance.now();
function frame(now) {
  const dt = Math.min((now - last) / 1000, 0.05);
  last = now;
  if (mode === "explore") movement(dt);
  else if (mode === "film") {
    if (!paused) filmTime += dt;
    film(filmTime);
  } else film(0);
  sky.position.copy(camera.position);
  for (const duck of ducks) {
    duck.g.position.y =
      duck.base +
      Math.sin(now * 0.0015 + duck.phase) * (duck.swim ? 0.035 : 0.012);
    if (duck.swim) duck.g.rotation.y += dt * 0.08;
    else duck.g.rotation.z = Math.sin(now * 0.0012 + duck.phase) * 0.028;
  }
  for (const { m, phase } of ripples) {
    const t = (now * 0.00025 + phase) % 1;
    m.scale.setScalar(0.3 + t * 1.1);
    m.material.opacity = rainOn ? (1 - t) * 0.2 : 0;
  }
  rain(dt);
  renderer.render(scene, camera);
  fpsTime += dt;
  fpsFrames++;
  if (fpsTime > 2.5 && mode !== "menu") {
    const fps = fpsFrames / fpsTime,
      max = Math.min(
        devicePixelRatio || 1,
        touch ? 1.3 : 1.6,
        Math.sqrt((touch ? 880000 : 1800000) / (innerWidth * innerHeight)),
      );
    if (fps < 53 && pixelRatio > 0.7)
      pixelRatio = Math.max(0.7, pixelRatio * 0.88);
    else if (fps > 59 && pixelRatio < max)
      pixelRatio = Math.min(max, pixelRatio * 1.04);
    renderer.setPixelRatio(pixelRatio);
    renderer.setSize(innerWidth, innerHeight);
    fpsTime = fpsFrames = 0;
  }
}
renderer.setAnimationLoop(frame);
Promise.allSettled([
  actualDucks(),
  actualPines(),
  actualBroadleaf(),
  actualPlants(),
]).then(() => {
  $("progress").style.width = "100%";
  $("load-note").textContent = "The creek is ready";
  setTimeout(() => {
    $("loading").hidden = true;
    $("menu").hidden = false;
  }, 250);
});
setTimeout(() => {
  if ($("loading").hidden) return;
  $("loading").hidden = true;
  $("menu").hidden = false;
}, 11000);

function foregroundGrass() {
  const amount = 1200,
    mesh = new THREE.InstancedMesh(
      new THREE.ConeGeometry(0.08, 1, 3),
      new THREE.MeshStandardMaterial({
        color: 0x568d44,
        roughness: 1,
        side: THREE.DoubleSide,
      }),
      amount,
    ),
    d = new THREE.Object3D(),
    c = new THREE.Color();
  let n = 0,
    attempt = 0;
  while (n < amount && attempt++ < 10000) {
    const x = rng(-7, 25),
      z = rng(-32, -8);
    if (radius(x, z) < 1.03 || Math.abs(x - (14 - 0.27 * (z + 20))) < 1.8)
      continue;
    const h = rng(0.25, 0.85);
    d.position.set(x, ground(x, z) + h * 0.42, z);
    d.scale.set(rng(0.7, 1.5), h, rng(0.6, 1.3));
    d.rotation.y = rng(0, 6.28);
    d.updateMatrix();
    mesh.setMatrixAt(n, d.matrix);
    c.setHSL(rng(0.27, 0.38), rng(0.4, 0.6), rng(0.2, 0.38));
    mesh.setColorAt(n++, c);
  }
  mesh.count = n;
  mesh.instanceMatrix.needsUpdate = true;
  mesh.instanceColor.needsUpdate = true;
  scene.add(mesh);
}
foregroundGrass();

function leafWash() {
  const c = document.createElement("canvas");
  c.width = c.height = 128;
  const p = c.getContext("2d");
  for (let i = 0; i < 220; i++) {
    const x = rng(0, 128),
      y = rng(0, 128),
      r = rng(4, 17);
    p.fillStyle = `rgba(${Math.round(rng(190, 240))},${Math.round(rng(206, 247))},${Math.round(rng(170, 220))},${rng(0.15, 0.5)})`;
    p.beginPath();
    p.ellipse(x, y, r, r * rng(0.45, 1), rng(0, 6), 0, Math.PI * 2);
    p.fill();
  }
  const t = new THREE.CanvasTexture(c);
  t.colorSpace = THREE.SRGBColorSpace;
  return t;
}
async function actualBroadleaf() {
  try {
    const original = await new FBXLoader(fbxManager).loadAsync(
        "/assets/bigtree-1.fbx",
      ),
      leafMat = new THREE.MeshStandardMaterial({
        map: leafWash(),
        color: 0x43884b,
        alphaTest: 0.22,
        side: THREE.DoubleSide,
        roughness: 1,
      }),
      barkMat = new THREE.MeshStandardMaterial({
        map: texture("tree-bark.webp"),
        color: 0x8b8b76,
        roughness: 1,
        side: THREE.DoubleSide,
      });
    original.traverse((child) => {
      if (/LOD[123]/i.test(child.name)) {
        child.visible = false;
        return;
      }
      if (!child.isMesh) return;
      const name = Array.isArray(child.material)
        ? child.material.map((m) => m.name).join(" ")
        : child.material?.name || "";
      const mat = /leaf|leaves|foliage/i.test(name) ? leafMat : barkMat;
      child.material = Array.isArray(child.material)
        ? child.material.map(() => mat)
        : mat;
    });
    const box = new THREE.Box3().setFromObject(original),
      size = box.getSize(new THREE.Vector3());
    if (!size.y || !Number.isFinite(size.y))
      throw Error("Invalid broadleaf bounds");
    for (const [x, z, h] of [
      [-10, -17, 17],
      [-29, -12, 14],
      [-31, 32, 13],
    ]) {
      const m = original.clone(true);
      m.scale.setScalar(h / size.y);
      m.position.set(x, ground(x, z) - box.min.y * m.scale.x, z);
      m.rotation.y = rng(0, 6.28);
      scene.add(m);
    }
    console.info("Original BK broadleaf meshes loaded");
  } catch (e) {
    console.warn("Using instanced broadleaf fallback", e);
  }
}
function farMossSheet() {
  const n = 240,
    m = new THREE.InstancedMesh(
      new THREE.CircleGeometry(1, 7),
      new THREE.MeshBasicMaterial({
        color: 0x9dc56a,
        side: THREE.DoubleSide,
        depthWrite: false,
      }),
      n,
    ),
    d = new THREE.Object3D(),
    c = new THREE.Color();
  let made = 0,
    attempt = 0;
  while (made < n && attempt++ < 3000) {
    const x = rng(-30, 30),
      z = rng(20, 43);
    if (radius(x, z) > 0.97) continue;
    d.position.set(x, 0.075 + made * 0.00001, z);
    d.rotation.set(-Math.PI / 2, 0, rng(0, 6.28));
    d.scale.set(rng(1.2, 3.8), rng(0.8, 2.8), 1);
    d.updateMatrix();
    m.setMatrixAt(made, d.matrix);
    c.setHSL(rng(0.21, 0.29), rng(0.32, 0.49), rng(0.39, 0.58));
    m.setColorAt(made++, c);
  }
  m.count = made;
  m.instanceMatrix.needsUpdate = true;
  m.instanceColor.needsUpdate = true;
  m.renderOrder = 3;
  scene.add(m);
}
farMossSheet();

async function actualPlants() {
  const assets = [
      ["grass-3.fbx", "grass-a.webp", 1100, 0.35, 0.85],
      ["fern-2.fbx", "fern-a.webp", 200, 0.8, 1.7],
      ["shrub-2.fbx", "shrub-a.webp", 140, 0.6, 1.25],
    ],
    fbx = new FBXLoader(fbxManager);
  for (const [file, image, count, minSize, maxSize] of assets) {
    try {
      const obj = await fbx.loadAsync("/assets/" + file);
      obj.updateMatrixWorld(true);
      let best = null;
      obj.traverse((m) => {
        if (
          m.isMesh &&
          (!best ||
            m.geometry.attributes.position.count >
              best.geometry.attributes.position.count)
        )
          best = m;
      });
      if (!best) throw Error("No mesh in " + file);
      const geo = best.geometry.clone();
      geo.applyMatrix4(best.matrixWorld);
      geo.computeBoundingBox();
      const b = geo.boundingBox,
        h = b.max.y - b.min.y;
      if (!h || !Number.isFinite(h)) throw Error("Invalid plant bounds");
      const cx = (b.min.x + b.max.x) / 2,
        cz = (b.min.z + b.max.z) / 2;
      geo.scale(1 / h, 1 / h, 1 / h);
      geo.translate(-cx / h, -b.min.y / h, -cz / h);
      geo.computeVertexNormals();
      const material = new THREE.MeshStandardMaterial({
          map: texture(image),
          color: 0x74a969,
          alphaTest: 0.25,
          side: THREE.DoubleSide,
          roughness: 1,
        }),
        mesh = new THREE.InstancedMesh(geo, material, count),
        d = new THREE.Object3D(),
        c = new THREE.Color();
      let made = 0,
        tries = 0;
      while (made < count && tries++ < count * 30) {
        let x, z;
        if (random() < 0.64) {
          x = rng(-8, 29);
          z = rng(-32, -7);
        } else {
          x = rng(-48, 48);
          z = rng(-29, 60);
        }
        if (
          radius(x, z) < 1.03 ||
          (Math.abs(x - (14 - 0.27 * (z + 20))) < 1.9 && z < 18)
        )
          continue;
        const y = ground(x, z),
          size = rng(minSize, maxSize);
        d.position.set(x, y, z);
        d.rotation.set(0, rng(0, 6.28), 0);
        d.scale.set(size, size, size);
        d.updateMatrix();
        mesh.setMatrixAt(made, d.matrix);
        c.setHSL(rng(0.27, 0.38), rng(0.4, 0.6), rng(0.35, 0.6));
        mesh.setColorAt(made++, c);
      }
      mesh.count = made;
      mesh.instanceMatrix.needsUpdate = true;
      mesh.instanceColor.needsUpdate = true;
      mesh.frustumCulled = false;
      scene.add(mesh);
      console.info("Original BK plant instanced", file, made);
    } catch (e) {
      console.warn("Plant model unavailable", file, e);
    }
  }
}
