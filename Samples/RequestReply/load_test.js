import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 1000,
  duration: '600s',
};

export default function() {
  const payload = JSON.stringify({
    firstName: 'Mr.Test',
    lastName: 'Test',
    email: 'test@test.com',
    locationId: 'db24f40e-5de4-405e-9887-c93dd1d860d4',
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };
  
  const res = http.post('http://localhost:5268/api/accounts', payload, params);
  check(res, { "status is 200": (res) => res.status === 200 });
  sleep(1);
}
