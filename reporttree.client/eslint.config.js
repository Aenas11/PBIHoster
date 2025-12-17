import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import vueTs from '@vue/eslint-config-typescript'
import globals from 'globals'

export default [
  {
    name: 'app/files-to-lint',
    files: ['**/*.{js,mjs,jsx,ts,tsx,vue}'],
  },

  {
    ignores: ['**/dist/**', '**/dist-ssr/**', '**/coverage/**'],
  },
  
  {
    languageOptions: {
      globals: {
        ...globals.browser,
      },
    },
  },

  js.configs.recommended,
  ...pluginVue.configs['flat/essential'],
  ...vueTs(),
  {
    rules: {
      'vue/no-deprecated-slot-attribute': 'off',
    }
  }
]
