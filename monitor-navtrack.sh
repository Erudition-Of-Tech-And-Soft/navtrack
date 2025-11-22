#!/bin/bash

###############################################################################
# NavTrack Monitoring Script
# Script para monitorear el estado de NavTrack y enviar alertas
# Puede ser ejecutado periódicamente mediante cron
###############################################################################

INSTALL_DIR="/opt/navtrack"
COMPOSE_FILE="docker-compose.prod.yml"
LOG_FILE="/var/log/navtrack-monitor.log"

# Umbrales de alerta
CPU_THRESHOLD=80
MEMORY_THRESHOLD=80
DISK_THRESHOLD=85

# Configuración de notificaciones (opcional)
ENABLE_EMAIL_ALERTS=false
ALERT_EMAIL="admin@inversionespereztaveras.com"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

send_alert() {
    local subject="$1"
    local message="$2"

    log "ALERT: $subject"

    if [ "$ENABLE_EMAIL_ALERTS" = true ]; then
        echo "$message" | mail -s "NavTrack Alert: $subject" "$ALERT_EMAIL"
    fi
}

check_container_health() {
    local container=$1
    local status=$(docker inspect --format='{{.State.Status}}' "$container" 2>/dev/null)

    if [ "$status" != "running" ]; then
        send_alert "Container $container is not running" "Container $container status: $status"
        return 1
    fi
    return 0
}

check_all_containers() {
    log "Checking container health..."

    cd "$INSTALL_DIR" || exit 1

    local containers=$(docker compose -f "$COMPOSE_FILE" ps --services)
    local all_healthy=true

    for container in $containers; do
        local full_name="navtrack-${container}"
        if [ "$container" = "frontend" ]; then full_name="navtrack-frontend"; fi
        if [ "$container" = "api" ]; then full_name="navtrack-api"; fi
        if [ "$container" = "odoo-api" ]; then full_name="navtrack-odoo-api"; fi
        if [ "$container" = "listener" ]; then full_name="navtrack-listener"; fi
        if [ "$container" = "database" ]; then full_name="navtrack-mongodb"; fi

        if ! check_container_health "$full_name"; then
            all_healthy=false
            log "Container $container is unhealthy!"
        fi
    done

    if [ "$all_healthy" = true ]; then
        log "All containers are healthy"
    fi
}

check_disk_space() {
    log "Checking disk space..."

    local disk_usage=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')

    if [ "$disk_usage" -gt "$DISK_THRESHOLD" ]; then
        send_alert "High disk usage" "Disk usage is at ${disk_usage}% (threshold: ${DISK_THRESHOLD}%)"
        return 1
    else
        log "Disk usage: ${disk_usage}%"
    fi
    return 0
}

check_memory_usage() {
    log "Checking memory usage..."

    local mem_usage=$(free | grep Mem | awk '{print int($3/$2 * 100)}')

    if [ "$mem_usage" -gt "$MEMORY_THRESHOLD" ]; then
        send_alert "High memory usage" "Memory usage is at ${mem_usage}% (threshold: ${MEMORY_THRESHOLD}%)"
        return 1
    else
        log "Memory usage: ${mem_usage}%"
    fi
    return 0
}

check_container_resources() {
    log "Checking container resource usage..."

    cd "$INSTALL_DIR" || exit 1

    docker stats --no-stream --format "{{.Container}}\t{{.CPUPerc}}\t{{.MemPerc}}" | while read -r line; do
        local container=$(echo "$line" | awk '{print $1}')
        local cpu=$(echo "$line" | awk '{print $2}' | sed 's/%//')
        local mem=$(echo "$line" | awk '{print $3}' | sed 's/%//')

        # Convertir a entero (eliminar decimales)
        cpu=${cpu%.*}
        mem=${mem%.*}

        if [ -n "$cpu" ] && [ "$cpu" -gt "$CPU_THRESHOLD" ]; then
            send_alert "High CPU usage on $container" "CPU usage is at ${cpu}% (threshold: ${CPU_THRESHOLD}%)"
        fi

        if [ -n "$mem" ] && [ "$mem" -gt "$MEMORY_THRESHOLD" ]; then
            send_alert "High memory usage on $container" "Memory usage is at ${mem}% (threshold: ${MEMORY_THRESHOLD}%)"
        fi
    done
}

check_ssl_certificates() {
    log "Checking SSL certificate expiration..."

    local domains=("gps-qa.inversionespereztaveras.com" "gps-api-qa.inversionespereztaveras.com" "gps-odoo-qa.inversionespereztaveras.com" "gps-listener-qa.inversionespereztaveras.com")

    for domain in "${domains[@]}"; do
        local cert_path="/etc/letsencrypt/live/$domain/cert.pem"

        if [ -f "$cert_path" ]; then
            local expiry_date=$(openssl x509 -enddate -noout -in "$cert_path" | cut -d= -f2)
            local expiry_epoch=$(date -d "$expiry_date" +%s)
            local now_epoch=$(date +%s)
            local days_left=$(( ($expiry_epoch - $now_epoch) / 86400 ))

            if [ "$days_left" -lt 7 ]; then
                send_alert "SSL certificate expiring soon for $domain" "Certificate expires in $days_left days"
            elif [ "$days_left" -lt 30 ]; then
                log "SSL certificate for $domain expires in $days_left days"
            fi
        fi
    done
}

check_nginx_status() {
    log "Checking Nginx status..."

    if ! systemctl is-active --quiet nginx; then
        send_alert "Nginx is not running" "Nginx service is down"
        return 1
    else
        log "Nginx is running"
    fi
    return 0
}

check_mongodb_connection() {
    log "Checking MongoDB connection..."

    cd "$INSTALL_DIR" || exit 1

    if docker compose -f "$COMPOSE_FILE" exec -T database mongosh --eval "db.adminCommand('ping')" navtrack > /dev/null 2>&1; then
        log "MongoDB is responding"
        return 0
    else
        send_alert "MongoDB not responding" "Cannot connect to MongoDB"
        return 1
    fi
}

check_listener_ports() {
    log "Checking GPS Listener ports..."

    # Verificar que al menos algunos puertos estén escuchando
    local listening_ports=$(netstat -tuln | grep -c ":700[0-9]")

    if [ "$listening_ports" -lt 5 ]; then
        send_alert "Few GPS Listener ports open" "Only $listening_ports listener ports are open"
        return 1
    else
        log "$listening_ports GPS listener ports are open"
    fi
    return 0
}

check_docker_volumes() {
    log "Checking Docker volumes..."

    cd "$INSTALL_DIR" || exit 1

    local volumes=$(docker compose -f "$COMPOSE_FILE" config --volumes)

    for volume in $volumes; do
        if ! docker volume inspect "$volume" > /dev/null 2>&1; then
            send_alert "Docker volume missing" "Volume $volume does not exist"
        fi
    done
}

generate_health_report() {
    log "Generating health report..."

    cat > /tmp/navtrack-health-report.txt <<EOF
NavTrack Health Report
Generated: $(date)

=== Container Status ===
$(docker ps --filter "name=navtrack" --format "table {{.Names}}\t{{.Status}}\t{{.Size}}")

=== Resource Usage ===
$(docker stats --no-stream --filter "name=navtrack" --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}")

=== Disk Usage ===
$(df -h / | tail -1)

=== System Memory ===
$(free -h)

=== Nginx Status ===
$(systemctl status nginx --no-pager | head -5)

=== Recent Logs (Last 20 lines per service) ===
EOF

    cd "$INSTALL_DIR" || exit 1

    for service in frontend api odoo-api listener database; do
        echo "" >> /tmp/navtrack-health-report.txt
        echo "=== $service ===" >> /tmp/navtrack-health-report.txt
        docker compose -f "$COMPOSE_FILE" logs --tail=20 "$service" >> /tmp/navtrack-health-report.txt 2>&1
    done

    log "Health report generated at /tmp/navtrack-health-report.txt"
}

run_all_checks() {
    log "========================================"
    log "Starting NavTrack health checks..."
    log "========================================"

    check_all_containers
    check_disk_space
    check_memory_usage
    check_container_resources
    check_nginx_status
    check_mongodb_connection
    check_listener_ports
    check_ssl_certificates
    check_docker_volumes

    log "========================================"
    log "Health checks completed"
    log "========================================"
}

show_help() {
    cat << EOF
NavTrack Monitoring Script

Uso: $(basename "$0") [comando]

Comandos:
  check           Ejecutar todas las verificaciones de salud
  report          Generar reporte completo de salud
  containers      Verificar estado de contenedores
  resources       Verificar uso de recursos
  ssl             Verificar certificados SSL
  mongodb         Verificar conexión a MongoDB
  help            Mostrar esta ayuda

Configuración de alertas:
  Edite las variables al inicio del script:
    - ENABLE_EMAIL_ALERTS: true/false
    - ALERT_EMAIL: email para recibir alertas
    - CPU_THRESHOLD: umbral de CPU (%)
    - MEMORY_THRESHOLD: umbral de memoria (%)
    - DISK_THRESHOLD: umbral de disco (%)

Configurar monitoreo automático (cron):
  # Ejecutar cada 5 minutos
  */5 * * * * /opt/navtrack/monitor-navtrack.sh check >> /var/log/navtrack-monitor.log 2>&1

  # Generar reporte diario a las 8 AM
  0 8 * * * /opt/navtrack/monitor-navtrack.sh report

EOF
}

###############################################################################
# Main
###############################################################################

case "$1" in
    check)
        run_all_checks
        ;;
    report)
        generate_health_report
        cat /tmp/navtrack-health-report.txt
        ;;
    containers)
        check_all_containers
        ;;
    resources)
        check_disk_space
        check_memory_usage
        check_container_resources
        ;;
    ssl)
        check_ssl_certificates
        ;;
    mongodb)
        check_mongodb_connection
        ;;
    help|--help|-h|"")
        show_help
        ;;
    *)
        echo "Comando desconocido: $1"
        echo ""
        show_help
        exit 1
        ;;
esac
