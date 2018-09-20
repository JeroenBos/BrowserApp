const nodeEnv = process.env.NODE_ENV || 'development';

module.exports = {
    devtool: 'source-map',
    entry: './tests/test.spec.ts',
    output: { filename: 'dist/test/index.js' },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: 'ts-loader'
            }
        ]
    },
    resolve: {
        extensions: ['.spec.ts'],
        alias: {
            vue: 'vue/dist/vue.esm.js'
        }
    },
    // Suppress fatal error: Cannot resolve module 'fs'
    // @relative https://github.com/pugjs/pug-loader/issues/8
    // @see https://github.com/webpack/docs/wiki/Configuration#node
    node: {
        fs: 'empty',
        child_process: 'empty'
    },
};