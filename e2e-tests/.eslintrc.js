module.exports = {
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 2022,
    sourceType: 'module',
  },
  plugins: ['@typescript-eslint'],
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
  ],
  env: {
    node: true,
    jest: true,
    es2022: true,
  },
  globals: {
    NodeJS: 'readonly',  // Allow NodeJS global namespace
  },
  rules: {
    'no-console': 'off', // Allow console in tests
    'prefer-const': 'error',
    'no-var': 'error',
    'no-unused-vars': 'off', // Turn off base rule as it conflicts with TypeScript
    '@typescript-eslint/no-explicit-any': 'off', // Allow any for test mocks
    '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
  },
  ignorePatterns: [
    'node_modules/',
    'dist/',
    'coverage/',
    '*.js'
  ],
};