const nodeEnv = process.env.NODE_ENV || 'development';
const CleanWebpackPlugin = require('clean-webpack-plugin');

module.exports = {
    devtool: 'source-map',
    entry: './tests/index.spec.ts',
    output: { filename: 'dist/webpacked.js' },
    module: {
        rules: [
            {
                test: /\.ts$/,
                loader: 'ts-loader'
            }
        ]
    },
    resolve: {
        extensions: ['.ts', '.js'],
    },
    // Suppress fatal error: Cannot resolve module 'fs'
    // @relative https://github.com/pugjs/pug-loader/issues/8
    // @see https://github.com/webpack/docs/wiki/Configuration#node
    node: {
        fs: 'empty',
        child_process: 'empty'
    },
    plugins: [
        new CleanWebpackPlugin(['dist'])
    ],
};