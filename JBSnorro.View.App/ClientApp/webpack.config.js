const nodeEnv = process.env.NODE_ENV || 'development';

module.exports = {
    devtool: 'source-map',
    entry: './test/test.spec.ts',
    output: { filename: 'D:/index/index.js' },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: 'ts-loader'
            }
        ]
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js'],
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