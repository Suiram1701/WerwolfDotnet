import { execSync } from 'child_process';

const command = `npx swagger-typescript-api --extract-enums -p "http://localhost:7216/swagger/v1/swagger.json" -o ./src/`;
const result = execSync(command, { encoding: 'utf-8' });
console.log(command);
console.log(result);
