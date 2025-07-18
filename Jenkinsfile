pipeline {
    agent any
    
environment {
    // Variables para SonarQube
    SONAR_PROJECT_KEY = 'mi-proyecto-csharp'
    SONAR_PROJECT_NAME = 'TitaFinal'
    SONAR_HOST_URL = 'http://localhost:9000'
    // Path para .NET y herramientas globales
    PATH = "${env.PATH};C:\\Program Files\\dotnet;%USERPROFILE%\\.dotnet\\tools"
}
    
    stages {
        stage('Checkout') {
            steps {
                echo 'Clonando repositorio...'
                checkout scm
            }
        }
        
        stage('Restore Dependencies') {
            steps {
                echo 'Restaurando dependencias de .NET...'
                bat 'dotnet restore'
            }
        }
        
        stage('SonarQube Analysis Start') {
            steps {
                echo 'Iniciando análisis de SonarQube...'
                withSonarQubeEnv('SonarQube') {
                    bat '''
                            dotnet-sonarscanner begin ^
                            /k:"%SONAR_PROJECT_KEY%" ^
                            /n:"%SONAR_PROJECT_NAME%" ^
                            /d:sonar.host.url="%SONAR_HOST_URL%" ^
                            /d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml" ^
                            /d:sonar.cs.vstest.reportsPaths="TestResults/*.trx" ^
                            /d:sonar.exclusions="**/bin/**,**/obj/**,**/*.dll,**/packages/**,**/wwwroot/lib/**"
                    '''
                }
            }
        }
        
        stage('Build') {
            steps {
                echo 'Compilando proyecto...'
                bat 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test & Coverage') {
            steps {
                echo 'Ejecutando pruebas y generando cobertura...'
                bat '''
                    dotnet test --no-restore --no-build --configuration Release ^
                        --logger trx ^
                        --collect:"XPlat Code Coverage" ^
                        --results-directory TestResults ^
                        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
                '''
            }
            post {
                always {
                    // Publicar resultados de tests
                    publishTestResults testResultsPattern: 'TestResults/*.trx'
                    
                    // Publicar cobertura (si tienes el plugin de Coverage)
                    publishCoverage adapters: [
                        openCoverageAdapter('TestResults/**/coverage.opencover.xml')
                    ], sourceFileResolver: sourceFiles('STORE_LAST_BUILD')
                }
            }
        }
        
        stage('SonarQube Analysis End') {
            steps {
                echo 'Finalizando análisis de SonarQube...'
                withSonarQubeEnv('SonarQube') {
bat 'dotnet-sonarscanner end'
                }
            }
        }
        
        stage('Quality Gate') {
            steps {
                echo 'Esperando resultado del Quality Gate...'
                timeout(time: 5, unit: 'MINUTES') {
                    waitForQualityGate abortPipeline: true
                }
            }
        }
        
        stage('Archive Artifacts') {
            steps {
                echo 'Archivando artefactos...'
                archiveArtifacts artifacts: 'TestResults/**/*', allowEmptyArchive: true
                archiveArtifacts artifacts: '**/bin/Release/**/*.dll', allowEmptyArchive: true
            }
        }
    }
    
    post {
        always {
            echo 'Pipeline completado'
            // Limpiar archivos temporales si es necesario
            bat 'if exist TestResults rmdir /s /q TestResults'
        }
        success {
            echo 'Pipeline ejecutado exitosamente!'
            echo 'Revisa la cobertura en SonarQube: ${SONAR_HOST_URL}/dashboard?id=${SONAR_PROJECT_KEY}'
        }
        failure {
            echo 'Pipeline falló. Revisar logs.'
            // Enviar notificación de error (opcional)
        }
        unstable {
            echo 'Pipeline inestable. Revisar Quality Gate.'
        }
    }
}
