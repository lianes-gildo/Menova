import http from 'k6/http';
import { check, sleep } from 'k6';

// Configuração do Teste de Carga do Menova POS (Metas p95 < 100ms, 30 dispositivos simultâneos, 20 req/s)
export const options = {
  stages: [
    { duration: '30s', target: 10 }, // Ramp-up para 10 garçons
    { duration: '1m', target: 30 },  // 30 dispositivos concorrentes (garçons, caixas, KDS)
    { duration: '30s', target: 0 },  // Ramp-down
  ],
  thresholds: {
    // 95% das operações do garçom devem responder abaixo de 100ms no servidor
    http_req_duration: ['p(95)<100'],
    http_req_failed: ['rate<0.01'], // Menos de 1% de falhas permitidas
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost';

export default function () {
  const params = {
    headers: {
      'Accept': 'application/json',
      'User-Agent': 'Menova-PWA-Garcom/1.0',
    },
  };

  // 1. Obter mapa de mesas da sala (Operação frequente do Garçom)
  const resMesas = http.get(`${BASE_URL}/api/v1/sala/mesas`, params);
  check(resMesas, {
    'Mapa de mesas status 200': (r) => r.status === 200,
    'Tempo de resposta mesas < 100ms': (r) => r.timings.duration < 100,
  });

  sleep(1);

  // 2. Obter catálogo de menu (Cache local do PWA com validação de versão)
  const resMenu = http.get(`${BASE_URL}/api/v1/menu/catalogo`, params);
  check(resMenu, {
    'Menu status 200': (r) => r.status === 200,
    'Tempo de resposta menu < 100ms': (r) => r.timings.duration < 100,
  });

  sleep(1);

  // 3. Consulta de tickets ativos no KDS da cozinha
  const resKds = http.get(`${BASE_URL}/api/v1/kds/tickets`, params);
  check(resKds, {
    'KDS tickets status 200': (r) => r.status === 200,
    'Tempo de resposta KDS < 100ms': (r) => r.timings.duration < 100,
  });

  sleep(1);
}
